param(
    [string]$BaseUrl = "http://localhost:8080",
    [switch]$KeepUser
)

$ErrorActionPreference = "Stop"

function Invoke-JsonRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body = $null,
        [hashtable]$Headers = $null,
        [int]$Depth = 10
    )

    try {
        $parameters = @{
            Method = $Method
            Uri = $Uri
            ContentType = "application/json"
        }

        if ($Headers) {
            $parameters.Headers = $Headers
        }

        if ($null -ne $Body) {
            $parameters.Body = ($Body | ConvertTo-Json -Depth $Depth)
        }

        return Invoke-RestMethod @parameters
    }
    catch {
        $response = $_.Exception.Response
        if ($response -and $response.GetResponseStream()) {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            throw "Request failed: $Method $Uri $([int]$response.StatusCode) $($response.StatusDescription) $responseBody"
        }

        throw
    }
}

$suffix = Get-Random -Maximum 999999
$email = "finance-conflict-$suffix@example.com"
$password = "Aa1!aaaa"
$cpf = (Get-Random -Minimum 10000000000 -Maximum 99999999999).ToString()

$user = @{
    fullname = "Finance Conflict User"
    email = $email
    ddd = 11
    phoneNumber = 999999999
    cpf_cnpj = $cpf
    zipcode = "01001000"
    street = "Rua Teste"
    number = "123"
    complement = ""
    neighborhood = "Centro"
    city = "Sao Paulo"
    state = "SP"
    password = $password
    confirmPassword = $password
}

Invoke-JsonRequest -Method Post -Uri "$BaseUrl/user" -Body $user | Out-Null

$login = Invoke-JsonRequest -Method Post -Uri "$BaseUrl/auth" -Body @{
    email = $email
    password = $password
}

$headers = @{ Authorization = "Bearer $($login.token)" }

$remoteState = @{
    incomes = @(
        @{
            id = "income-conflict"
            description = "Remote income"
            amount = 100
            category = "Remote"
        }
    )
    expenses = @()
    cards = @()
    goals = @()
    accounts = @()
    investments = @()
}

$saved = Invoke-JsonRequest -Method Put -Uri "$BaseUrl/api/finance/state" -Headers $headers -Body $remoteState

$localState = @{
    incomes = @(
        @{
            id = "income-conflict"
            description = "Local income"
            amount = 200
            category = "Local"
        }
    )
    expenses = @()
    cards = @()
    goals = @()
    accounts = @()
    investments = @()
}

$sync = Invoke-JsonRequest -Method Post -Uri "$BaseUrl/api/finance/sync" -Headers $headers -Body @{
    baseVersion = 0
    localState = $localState
}

if ($sync.source -ne "merged") {
    throw "Expected sync source 'merged', got '$($sync.source)'."
}

if ($sync.state.incomes[0].amount -ne 100) {
    throw "Expected remote amount 100 to win, got '$($sync.state.incomes[0].amount)'."
}

if ($sync.conflicts.Count -lt 3) {
    throw "Expected at least 3 conflicts, got '$($sync.conflicts.Count)'."
}

$deletedUser = $false
if (-not $KeepUser) {
    Invoke-JsonRequest -Method Delete -Uri "$BaseUrl/user" -Headers $headers | Out-Null
    $deletedUser = $true
}

[pscustomobject]@{
    email = $email
    savedVersion = $saved.serverVersion
    syncSource = $sync.source
    syncVersion = $sync.serverVersion
    winningAmount = $sync.state.incomes[0].amount
    conflictFields = @($sync.conflicts | ForEach-Object { $_.field })
    conflicts = $sync.conflicts.Count
    deletedUser = $deletedUser
} | ConvertTo-Json -Depth 5

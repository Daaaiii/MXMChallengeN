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
$email = "finance-e2e-$suffix@example.com"
$password = "Aa1!aaaa"
$cpf = (Get-Random -Minimum 10000000000 -Maximum 99999999999).ToString()

$user = @{
    fullname = "Finance Ete User"
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
$initial = Invoke-JsonRequest -Method Get -Uri "$BaseUrl/api/finance/state" -Headers $headers

$state = @{
    incomes = @(
        @{
            id = "income-e2e"
            description = "Salario E2E"
            amount = 1234
            date = "2026-06-12"
            category = "Salario"
            recurring = $false
            active = $true
            createdAt = "2026-06-12T00:00:00.000Z"
            updatedAt = "2026-06-12T01:00:00.000Z"
            version = 1
        }
    )
    expenses = @()
    cards = @()
    goals = @()
    accounts = @()
    investments = @()
}

$saved = Invoke-JsonRequest -Method Put -Uri "$BaseUrl/api/finance/state" -Headers $headers -Body $state

$state.incomes[0].amount = 1300
$state.incomes[0].version = 2
$state.incomes[0].updatedAt = "2026-06-12T02:00:00.000Z"

$sync = Invoke-JsonRequest -Method Post -Uri "$BaseUrl/api/finance/sync" -Headers $headers -Body @{
    baseVersion = $saved.serverVersion
    localState = $state
}

$deletedUser = $false
if (-not $KeepUser) {
    Invoke-JsonRequest -Method Delete -Uri "$BaseUrl/user" -Headers $headers | Out-Null
    $deletedUser = $true
}

[pscustomobject]@{
    email = $email
    initialExists = $initial.exists
    savedVersion = $saved.serverVersion
    syncSource = $sync.source
    syncVersion = $sync.serverVersion
    incomeAmount = $sync.state.incomes[0].amount
    conflicts = $sync.conflicts.Count
    deletedUser = $deletedUser
} | ConvertTo-Json -Depth 5

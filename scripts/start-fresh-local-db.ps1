param(
    [switch]$RotatePassword
)

$ErrorActionPreference = 'Stop'

Set-Location (Split-Path -Parent $PSScriptRoot)

$envFile = Join-Path (Get-Location) '.env'
$envLines = @(Get-Content $envFile -ErrorAction SilentlyContinue)
$passwordIndex = -1
for ($index = 0; $index -lt $envLines.Count; $index++) {
    if ($envLines[$index] -match '^MSSQL_SA_PASSWORD=') {
        $passwordIndex = $index
        break
    }
}

if ($RotatePassword -or $passwordIndex -lt 0) {
    $generatedPassword = 'KTraffic!1' + [guid]::NewGuid().ToString('N')
    $utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
    $passwordEntry = "MSSQL_SA_PASSWORD=$generatedPassword"
    if ($passwordIndex -lt 0) {
        $envLines += $passwordEntry
    }
    else {
        $envLines[$passwordIndex] = $passwordEntry
    }
    [System.IO.File]::WriteAllLines($envFile, $envLines, $utf8WithoutBom)
}
else {
    $passwordEntry = $envLines[$passwordIndex]
}

$env:MSSQL_SA_PASSWORD = $passwordEntry.Substring('MSSQL_SA_PASSWORD='.Length)

try {
    $composeConfig = docker compose config --format json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Compose configuration is invalid.'
    }
    if ($composeConfig.services.database.environment.MSSQL_SA_PASSWORD -cne $env:MSSQL_SA_PASSWORD) {
        throw 'Compose is not using the password saved in the root .env file.'
    }

    docker compose down
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not stop the current Compose containers.'
    }

    docker compose up --build -d --wait --wait-timeout 180
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not start the fresh local stack.'
    }

    docker compose ps
}
finally {
    Remove-Item Env:\MSSQL_SA_PASSWORD -ErrorAction SilentlyContinue
}
$ErrorActionPreference = 'Stop'
$taskSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../Assets/Scripts/PlayerVitalsState.cs') -Raw
Add-Type -TypeDefinition $taskSource

function Assert-Near([float]$actual, [float]$expected, [string]$message) {
    if ([Math]::Abs($actual - $expected) -gt 0.001) { throw "$message : $actual instead of $expected" }
}
function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}

$taskState = [ToxicDepths.PlayerVitalsState]::new()
Assert-True ($taskState.TrySpendStamina(12)) 'A rested player must be able to swing.'
Assert-Near $taskState.Stamina 88 'Swing stamina cost'
Assert-Near $taskState.Energy 99.4 'Swing uses the existing stamina-to-energy rule'
Assert-Near $taskState.MaxStamina 99.82 'Energy loss reduces stamina capacity'
[void]$taskState.TickSprint($false, 0.5)
Assert-Near $taskState.Stamina 88 'Mining shares the stamina regeneration delay'
[void]$taskState.TickSprint($false, 0.5)
Assert-Near $taskState.Stamina 94 'Only time after the delay regenerates stamina'

$taskState = [ToxicDepths.PlayerVitalsState]::new()
[void]$taskState.TrySpendStamina(95)
$taskEnergyBefore = $taskState.Energy
$taskStaminaBefore = $taskState.Stamina
Assert-True (-not $taskState.TrySpendStamina(12)) 'A partial swing must be rejected.'
Assert-Near $taskState.Stamina $taskStaminaBefore 'Rejected swing preserves stamina'
Assert-Near $taskState.Energy $taskEnergyBefore 'Rejected swing preserves energy'

$taskState = [ToxicDepths.PlayerVitalsState]::new(100, 1, 0.7, 20, 30, 0.8, 0.05, 0.2)
Assert-True ($taskState.TrySpendStamina(12)) 'Enough energy for first swing'
$taskStaminaBefore = $taskState.Stamina
Assert-True (-not $taskState.TrySpendStamina(12)) 'Insufficient energy blocks another swing'
Assert-Near $taskState.Energy 0.4 'Energy rejection is atomic'
Assert-Near $taskState.Stamina $taskStaminaBefore 'Energy rejection preserves stamina'
$taskState.RestoreEnergy(0.6)
Assert-True ($taskState.TrySpendStamina(12)) 'Restoring energy permits mining again'

$taskState = [ToxicDepths.PlayerVitalsState]::new()
[void]$taskState.ConsumeStamina(100)
[void]$taskState.TickSprint($false, 0.9)
Assert-True (-not $taskState.TrySpendStamina(1)) 'Mining respects the exhaustion recovery threshold'
[void]$taskState.TickSprint($false, 1)
Assert-True ($taskState.TrySpendStamina(12)) 'Mining resumes after recovery'
foreach ($taskInvalidCost in @(-1, [float]::NaN, [float]::PositiveInfinity)) {
    Assert-True (-not $taskState.TrySpendStamina($taskInvalidCost)) 'Invalid action cost must not alter resources'
}
Write-Output 'PASS: mining costs, energy/capacity coupling, atomic rejection, shared recovery and exhaustion.'

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [string] $Tags,
    [Parameter(Mandatory = $false)]
    [string] $Labels,
    [Parameter(Mandatory = $false)]
    [switch] $WhatIf
)

Write-Output "Powershell Version: $($PSVersionTable.PSVersion)"
Write-Output "Tags (raw): $Tags"
Write-Output "Labels (raw): $Labels"

$tagStrs = $Tags.Trim() -split '\r|\n|;' | Where-Object { $_ }
$labelStrs = $Labels.Trim() -split '\r|\n|;' | Where-Object { $_ }

Write-Output "Tags: $tagStrs"
Write-Output "Labels: $labelStrs"

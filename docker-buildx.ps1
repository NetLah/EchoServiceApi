[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [string] $Context = '.',
    [Parameter(Mandatory = $false)]
    [string] $Tags,
    [Parameter(Mandatory = $false)]
    [string] $Labels,
    [Parameter(Mandatory = $false)]
    [switch] $NoPush
)

Write-Output "Powershell Version: $($PSVersionTable.PSVersion)"
Write-Output "Tags (raw): $Tags"
Write-Output "Labels (raw): $Labels"

$tagStrs = $Tags.Trim() -split '\r|\n|;|,' | Where-Object { $_ }
$labelStrs = $Labels.Trim() -split '\r|\n|;|,' | Where-Object { $_ }

if (!$tagStrs) {
    throw 'Tags is required'
}

$params = @('buildx', 'build', '--pull')

if ($labelStrs) {
    $params += $labelStrs | ForEach-Object { @('--label', $_) }
}

if ($tagStrs) {
    $params += $tagStrs | ForEach-Object { @('--tag', $_) }
}

if (!$NoPush) {
    $params += @('--push')
}

docker @params $Context
exit $LASTEXITCODE

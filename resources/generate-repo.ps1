$pluginsOut = @()

$username = "Meisterlala"
$repo = "NekoFans"

$nekoname = "NekoFans.zip"
$nekolewdname = "NekoFansLewd.zip"

$nekolewdtag = "1.0.8-NSFW"

# Authorization header for the Github API.
$header = @{
  "Authorization" = "Bearer $env:GITHUB_TOKEN"
}

##############
# Neko Fans
##############

# Fetch the release data from the Gibhub API
$data = Invoke-WebRequest -Uri "https://api.github.com/repos/$($username)/$($repo)/releases/latest" -Headers $header
$json = ConvertFrom-Json $data.content

# Select Neko Fans zip
$asset = $json.assets | Where-Object { $_.Name -eq $nekoname } #Darts

# Get data from the api request.
$count = $asset.download_count
$download = $asset.browser_download_url

# Get timestamp for the release.
$time = [Int](New-TimeSpan -Start (Get-Date "01/01/1970") -End ([DateTime]$json.published_at)).TotalSeconds

# Download the zip file.
Invoke-WebRequest -Uri $download -OutFile "$env:Temp/NekoFans.zip"
Expand-Archive -Path "$env:Temp/NekoFans.zip" -DestinationPath "$env:Temp/NekoFans" -Force

# Load the json from the release.zip
$config = Get-Content -Path "$env:Temp/NekoFans/Neko.json" | Out-String | ConvertFrom-Json

# remove tmp files
Remove-Item -Path "$env:Temp/NekoFans.zip" -Force
Remove-Item -Path "$env:Temp/NekoFans" -Force -Recurse


# Add additional properties to the config.
$config | Add-Member -Name "IsHide" -MemberType NoteProperty -Value "False"
$config | Add-Member -Name "IsTestingExclusive" -MemberType NoteProperty -Value "False"
$config | Add-Member -Name "LastUpdated" -MemberType NoteProperty -Value $time
$config | Add-Member -Name "DownloadCount" -MemberType NoteProperty -Value $count
$config | Add-Member -Name "DownloadLinkInstall" -MemberType NoteProperty -Value $download
$config | Add-Member -Name "DownloadLinkTesting" -MemberType NoteProperty -Value $download
$config | Add-Member -Name "DownloadLinkUpdate" -MemberType NoteProperty -Value $download


# Add to the plugin array.
$pluginsOut += $config


##############
# Neko Lewd
##############

# Fetch the release data from the Gibhub API
$dataL = Invoke-WebRequest -Uri "https://api.github.com/repos/$($username)/$($repo)/releases/tags/$($nekolewdtag)" -Headers $header
$jsonL = ConvertFrom-Json $dataL.content

# Select Neko Fans zip
$assetL = $jsonL.assets | Where-Object { $_.Name -eq $nekolewdname }

# Get data from the api request.
$countL = $assetL.download_count
$downloadL = $assetL.browser_download_url

# Get timestamp for the release.
$timeL = [Int](New-TimeSpan -Start (Get-Date "01/01/1970") -End ([DateTime]$jsonL.published_at)).TotalSeconds

# Download the zip file.
Invoke-WebRequest -Uri $downloadL -OutFile "$env:Temp/NekoLewd.zip"
Expand-Archive -Path "$env:Temp/NekoLewd.zip" -DestinationPath "$env:Temp/NekoLewd" -Force

# Load the json from the release.zip
$configL = Get-Content -Path "$env:Temp/NekoLewd/NekoLewd.json"  | Out-String | ConvertFrom-Json

# remove tmp files
Remove-Item -Path "$env:Temp/NekoLewd.zip" -Force
Remove-Item -Path "$env:Temp/NekoLewd" -Force -Recurse


# Add additional properties to the config.
$configL | Add-Member -Name "IsHide" -MemberType NoteProperty -Value "False"
$configL | Add-Member -Name "IsTestingExclusive" -MemberType NoteProperty -Value "False"
$configL | Add-Member -Name "LastUpdated" -MemberType NoteProperty -Value $timeL
$configL | Add-Member -Name "DownloadCount" -MemberType NoteProperty -Value $countL
$configL | Add-Member -Name "DownloadLinkInstall" -MemberType NoteProperty -Value $downloadL
$configL | Add-Member -Name "DownloadLinkTesting" -MemberType NoteProperty -Value $downloadL
$configL | Add-Member -Name "DownloadLinkUpdate" -MemberType NoteProperty -Value $downloadL
$configL | Add-Member -Name "IsThirdParty" -MemberType NoteProperty -Value "True" -Force


# Add to the plugin array.
$pluginsOut += $configL


# Convert plugins to JSON
$pluginJson = ConvertTo-Json $pluginsOut

# Save repo to file
Set-Content -Path "repo.json" -Value $pluginJson

# Function to exit with a specific code.
function ExitWithCode($code) {
  $host.SetShouldExit($code)
  exit $code
}

[CmdletBinding()]
param(
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path (Join-Path $PSScriptRoot "..") "Resources\Audio"
}

Add-Type -AssemblyName System.Speech

function Get-MaleEnglishVoiceName {
    param([System.Speech.Synthesis.SpeechSynthesizer]$Synth)

    $voice = $Synth.GetInstalledVoices() |
        ForEach-Object { $_.VoiceInfo } |
        Where-Object { $_.Culture.Name -like 'en-*' -and $_.Gender -eq 'Male' } |
        Select-Object -First 1

    if ($voice) {
        return $voice.Name
    }

    return $null
}

function Pad-WaveToDuration {
    param(
        [string]$Path,
        [int]$TargetMilliseconds
    )

    [byte[]]$bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 44) {
        return
    }

    $byteRate = [BitConverter]::ToInt32($bytes, 28)
    $dataSize = [BitConverter]::ToInt32($bytes, 40)
    if ($byteRate -le 0 -or $dataSize -le 0) {
        return
    }

    $targetDataSize = [int][Math]::Ceiling($byteRate * ($TargetMilliseconds / 1000.0))
    if ($targetDataSize -le $dataSize) {
        return
    }

    $newBytes = New-Object byte[] (44 + $targetDataSize)
    [Array]::Copy($bytes, 0, $newBytes, 0, $bytes.Length)
    [BitConverter]::GetBytes(36 + $targetDataSize).CopyTo($newBytes, 4)
    [BitConverter]::GetBytes($targetDataSize).CopyTo($newBytes, 40)
    [System.IO.File]::WriteAllBytes($Path, $newBytes)
}

function New-VoiceAsset {
    param(
        [string]$FileName,
        [string]$Text,
        [int]$TargetMilliseconds,
        [int]$Rate = -2,
        [int]$Volume = 100
    )

    $path = Join-Path $OutputDirectory $FileName
    $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer

    try {
        $voiceName = Get-MaleEnglishVoiceName -Synth $synth
        if ($voiceName) {
            $synth.SelectVoice($voiceName)
        }

        $synth.Rate = $Rate
        $synth.Volume = $Volume
        $synth.SetOutputToWaveFile($path)
        $synth.Speak($Text)
    }
    finally {
        $synth.Dispose()
    }

    Pad-WaveToDuration -Path $path -TargetMilliseconds $TargetMilliseconds
    Write-Host "Generated $path"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

New-VoiceAsset -FileName "Echo_startup.wav" -Text "Created by Echoing 18 22 Tide." -TargetMilliseconds 7000 -Rate -3
New-VoiceAsset -FileName "VS_Code_startup.wav" -Text "Developed with V S Code." -TargetMilliseconds 8000 -Rate -3
New-VoiceAsset -FileName "Title.wav" -Text "Maui Battleship." -TargetMilliseconds 7000 -Rate -2
New-VoiceAsset -FileName "Target_Hit.wav" -Text "Target hit!" -TargetMilliseconds 1800 -Rate -2
New-VoiceAsset -FileName "Target_miss.wav" -Text "Target miss." -TargetMilliseconds 1800 -Rate -2

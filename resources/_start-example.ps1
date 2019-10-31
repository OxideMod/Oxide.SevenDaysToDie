Clear-Host

$root = $PSScriptRoot
$executable = "7DaysToDieServer.exe"
$args = "-batchmode -nographics -logfile $root\output_log.txt -configfile=serverconfig.xml -dedicated"

Write-Host "Starting server..."
Do {
    Try {
        Get-Process $executable -ErrorAction Stop | Out-Null
    }
    Catch {
        Start-Process $executable -ArgumentList $args -NoNewWindow -Wait
    }
    Write-Host `n
    Write-Host "Restarting server..."
    Start-Sleep -Seconds 10
    Write-Host `n
} While (Get-Process $executable)

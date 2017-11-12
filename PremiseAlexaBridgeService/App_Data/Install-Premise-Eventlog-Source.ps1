function Create-EventSources() {
    $eventSources = @("Application","PremiseBridge" )
    foreach ($source in $eventSources) {
            if ([System.Diagnostics.EventLog]::SourceExists($source) -eq $false) {
                [System.Diagnostics.EventLog]::CreateEventSource($source, "Application")
            }
    }
}

Create-EventSources


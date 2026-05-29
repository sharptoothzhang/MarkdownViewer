# Download mermaid.min.js for local bundling
$url = "https://cdn.jsdelivr.net/npm/mermaid@10.2.3/dist/mermaid.min.js"
$output = "mermaid.min.js"

try {
    Invoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing
    Write-Host "Downloaded mermaid.min.js successfully"
} catch {
    Write-Host "Failed to download: $_"
}

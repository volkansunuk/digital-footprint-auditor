# API Örnekleri

Bu örnekler başlangıç taslağıdır; geliştirme sırasında gerekçeli olarak değiştirilebilir.

## Tarama başlatma

```http
POST /api/scans
Content-Type: application/json
```

```json
{
  "email": "user@example.com",
  "githubUsername": "sample-user",
  "domain": "example.com",
  "websiteUrl": "https://example.com"
}
```

Alanların hiçbiri tek başına zorunlu değildir; en az bir hedef girilmelidir.

Cevap `201 Created`:

```json
{
  "id": "2e24c639-4bbf-4e0d-9dc3-b7b1d2789e5c",
  "status": "Running",
  "createdAt": "2026-07-20T10:00:00Z"
}
```

## Tarama sonucu

```http
GET /api/scans/{id}
```

```json
{
  "id": "2e24c639-4bbf-4e0d-9dc3-b7b1d2789e5c",
  "status": "Completed",
  "riskScore": 35,
  "riskLevel": "Medium",
  "findings": [
    {
      "scannerName": "DnsSecurityScanner",
      "title": "DMARC record not found",
      "description": "The domain does not currently publish a DMARC record.",
      "severity": "Medium",
      "scoreImpact": 15
    },
    {
      "scannerName": "GitHubProfileScanner",
      "title": "Public email visible on GitHub profile",
      "description": "Your GitHub profile exposes an email address publicly.",
      "severity": "Low",
      "scoreImpact": 10
    }
  ]
}
```

## Diğer endpointler

```http
GET    /api/scans                → tarama listesi (sayfalama düşünün)
DELETE /api/scans/{id}           → tarama silme
GET    /api/scans/{id}/findings  → yalnızca bulgular
```

## Hata cevapları

Hatalarda [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) formatını kullanmayı
araştırın — ASP.NET Core bunu hazır destekler:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Scan with id '...' was not found."
}
```

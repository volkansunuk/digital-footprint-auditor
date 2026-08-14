loadScanDetail();

function getScanIdFromUrl() {
    const params = new URLSearchParams(window.location.search);
    return params.get('id');
}

async function loadScanDetail() {
    const container = document.getElementById('detailContainer');
    const scanId = getScanIdFromUrl();

    if (!scanId) {
        showMessage(container, 'Geçersiz tarama adresi.', 'error');
        return;
    }

    try {
        const response = await fetch(`/api/scans/${scanId}`);

        if (response.status === 404) {
            showMessage(container, 'Bu tarama bulunamadı.', 'error');
            return;
        }

        if (!response.ok) {
            showMessage(container, 'Tarama detayı yüklenemedi.', 'error');
            return;
        }

        const scan = await response.json();
        renderDetail(scan);
    } catch (error) {
        showMessage(container, `Beklenmeyen bir hata oluştu: ${error.message}`, 'error');
    }
}

function renderDetail(scan) {
    const container = document.getElementById('detailContainer');
    const statusLabels = ['Beklemede', 'Çalışıyor', 'Tamamlandı', 'Kısmen Tamamlandı', 'Başarısız'];
    const riskLabels = ['Düşük', 'Orta', 'Yüksek'];
    const targetTypeLabels = ['Email', 'GitHub Kullanıcı Adı', 'Domain', 'Website'];

    container.replaceChildren();
    appendTextElement(container, 'p', `Durum: ${statusLabels[scan.status] ?? 'Bilinmiyor'}`);
    appendTextElement(container, 'p', `Risk Puanı: ${scan.riskScore} / 100 (${riskLabels[scan.riskLevel] ?? 'Bilinmiyor'})`);
    appendTextElement(container, 'p', `Oluşturulma: ${new Date(scan.createdAt).toLocaleString('tr-TR')}`);
    appendTextElement(container, 'h2', 'Hedefler');
    const targets = document.createElement('ul');
    scan.targets.forEach(target => {
        appendTextElement(targets, 'li', `${targetTypeLabels[target.targetType] ?? 'Bilinmiyor'}: ${target.targetValue}`);
    });
    container.appendChild(targets);
    appendTextElement(container, 'h2', 'Bulgular');
    if (scan.findings.length === 0) {
        appendTextElement(container, 'p', 'Hiçbir bulgu bulunamadı.');
    } else {
        scan.findings.forEach(finding => {
            const severityClass = finding.severity >= 3 ? 'severity-high' : finding.severity >= 2 ? 'severity-medium' : '';
            const findingElement = document.createElement('div');
            findingElement.className = `finding ${severityClass}`.trim();
            appendTextElement(findingElement, 'strong', finding.title);
            appendTextElement(findingElement, 'span', finding.scannerName, 'scanner-tag');
            appendTextElement(findingElement, 'p', finding.description);
            container.appendChild(findingElement);
        });
    }
}

function showMessage(container, message, className = '') {
    container.replaceChildren();
    appendTextElement(container, 'p', message, className);
}

function appendTextElement(parent, tagName, text, className = '') {
    const element = document.createElement(tagName);
    element.textContent = text;
    element.className = className;
    parent.appendChild(element);
    return element;
}

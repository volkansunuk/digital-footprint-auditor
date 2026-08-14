loadScanHistory();

async function loadScanHistory() {
    const container = document.getElementById('historyContainer');

    try {
        const response = await fetch('/api/scans');

        if (!response.ok) {
            showMessage(container, 'Tarama geçmişi yüklenemedi.', 'error');
            return;
        }

        const scans = await response.json();
        renderHistory(scans);
    } catch (error) {
        showMessage(container, `Beklenmeyen bir hata oluştu: ${error.message}`, 'error');
    }
}

function renderHistory(scans) {
    const container = document.getElementById('historyContainer');

    if (scans.length === 0) {
        showMessage(container, 'Henüz hiç tarama yapılmamış.');
        return;
    }

    const statusLabels = ['Beklemede', 'Çalışıyor', 'Tamamlandı', 'Kısmen Tamamlandı', 'Başarısız'];
    const riskLabels = ['Düşük', 'Orta', 'Yüksek'];

    const sortedScans = scans.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

    const table = document.createElement('table');
    table.className = 'history-table';
    const headerRow = document.createElement('tr');
    ['Tarih', 'Durum', 'Risk', 'Hedef Sayısı'].forEach(label => appendTextElement(headerRow, 'th', label));
    const head = document.createElement('thead');
    head.appendChild(headerRow);
    const body = document.createElement('tbody');
    sortedScans.forEach(scan => {
        const date = new Date(scan.createdAt).toLocaleString('tr-TR');
        const row = document.createElement('tr');
        row.addEventListener('click', () => {
            window.location.href = `detail.html?id=${encodeURIComponent(scan.id)}`;
        });
        appendTextElement(row, 'td', date);
        appendTextElement(row, 'td', statusLabels[scan.status] ?? 'Bilinmiyor');
        appendTextElement(row, 'td', `${riskLabels[scan.riskLevel] ?? 'Bilinmiyor'} (${scan.riskScore})`);
        appendTextElement(row, 'td', scan.targets.length);
        body.appendChild(row);
    });
    table.append(head, body);
    container.replaceChildren(table);
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

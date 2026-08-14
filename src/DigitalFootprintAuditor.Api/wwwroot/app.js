document.getElementById('addTargetBtn').addEventListener('click', addTargetRow);
document.getElementById('scanForm').addEventListener('submit', submitScan);

function addTargetRow() {
    const container = document.getElementById('targetsContainer');
    const row = document.createElement('div');
    row.className = 'target-row';
    row.innerHTML = `
        <select class="target-type">
            <option value="0">Email</option>
            <option value="1">GitHub Kullanıcı Adı</option>
            <option value="2">Domain</option>
            <option value="3">Website</option>
        </select>
        <input type="text" class="target-value" placeholder="Hedef değeri girin" required>
        <button type="button" class="remove-btn" onclick="removeTargetRow(this)">Sil</button>
    `;
    container.appendChild(row);
}

function removeTargetRow(button) {
    button.parentElement.remove();
}

async function submitScan(event) {
    event.preventDefault();

    const targetRows = document.querySelectorAll('.target-row');
    const targets = Array.from(targetRows).map(row => ({
        targetType: parseInt(row.querySelector('.target-type').value),
        targetValue: row.querySelector('.target-value').value
    }));

    const resultContainer = document.getElementById('resultContainer');
    showMessage(resultContainer, 'Tarama başlatıldı, lütfen bekleyin...');

    try {
        const response = await fetch('/api/scans', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ targets })
        });

        if (!response.ok) {
            const errorData = await response.json();
            showMessage(resultContainer, `Hata: ${JSON.stringify(errorData.errors)}`, 'error');
            return;
        }

        const scan = await response.json();
        renderScanResult(scan);
    } catch (error) {
        showMessage(resultContainer, `Beklenmeyen bir hata oluştu: ${error.message}`, 'error');
    }
}

function renderScanResult(scan) {
    const resultContainer = document.getElementById('resultContainer');
    resultContainer.replaceChildren();
    appendTextElement(resultContainer, 'h2', 'Tarama Sonucu');
    appendTextElement(resultContainer, 'p', `Risk Puanı: ${scan.riskScore} / 100`);
    appendTextElement(resultContainer, 'p', 'Bulgular:');

    if (scan.findings.length === 0) {
        appendTextElement(resultContainer, 'p', 'Hiçbir bulgu bulunamadı.');
    } else {
        scan.findings.forEach(finding => {
            const severityClass = finding.severity >= 3 ? 'severity-high' : finding.severity >= 2 ? 'severity-medium' : '';
            const findingElement = document.createElement('div');
            findingElement.className = `finding ${severityClass}`.trim();
            appendTextElement(findingElement, 'strong', finding.title);
            appendTextElement(findingElement, 'p', finding.description);
            resultContainer.appendChild(findingElement);
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

window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

window.exportDivToImage = async (divId, fileName) => {
    const element = document.getElementById(divId);
    if (!element) return;
    
    // We add html2canvas script dynamically if not present
    if (typeof html2canvas === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js';
        script.onload = () => captureAndDownload(element, fileName);
        document.head.appendChild(script);
    } else {
        await captureAndDownload(element, fileName);
    }
}

async function captureAndDownload(element, fileName) {
    const hiddenElements = element.querySelectorAll('.print-hide');
    const originalDisplays = [];
    hiddenElements.forEach(el => {
        originalDisplays.push(el.style.display);
        el.style.display = 'none';
    });

    const canvas = await html2canvas(element, { 
        scale: 3, 
        useCORS: true
    });

    hiddenElements.forEach((el, index) => {
        el.style.display = originalDisplays[index];
    });

    const url = canvas.toDataURL("image/png");
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? 'export.png';
    anchorElement.click();
    anchorElement.remove();
}

window.copyDivToClipboardImage = async (divId) => {
    const element = document.getElementById(divId);
    if (!element) return false;
    
    if (typeof html2canvas === 'undefined') {
        return new Promise((resolve) => {
            const script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js';
            script.onload = () => captureAndCopy(element).then(resolve);
            document.head.appendChild(script);
        });
    } else {
        return await captureAndCopy(element);
    }
}

async function captureAndCopy(element) {
    try {
        const hiddenElements = element.querySelectorAll('.print-hide');
        const originalDisplays = [];
        hiddenElements.forEach(el => {
            originalDisplays.push(el.style.display);
            el.style.display = 'none';
        });

        const canvas = await html2canvas(element, { 
            scale: 3, 
            useCORS: true
        });

        hiddenElements.forEach((el, index) => {
            el.style.display = originalDisplays[index];
        });

        return new Promise((resolve) => {
            canvas.toBlob(async (blob) => {
                try {
                    const item = new window.ClipboardItem({ 'image/png': blob });
                    await navigator.clipboard.write([item]);
                    resolve(true);
                } catch (err) {
                    console.error('Failed to copy image: ', err);
                    resolve(false);
                }
            }, 'image/png');
        });
    } catch (err) {
        console.error('Failed to capture image: ', err);
        return false;
    }
}

window.cmClipboard = {
    initialize: function (dotNetReference, elementId, targetId) {
        const element = document.getElementById(elementId);
        if (!element) return;
        
        element.onpaste = (e) => {
            const items = (e.clipboardData || e.originalEvent.clipboardData).items;
            for (let i = 0; i < items.length; i++) {
                const item = items[i];
                if (item.kind === 'file' && item.type.startsWith('image/')) {
                    const blob = item.getAsFile();
                    const reader = new FileReader();
                    reader.onload = (event) => {
                        const base64Str = event.target.result;
                        dotNetReference.invokeMethodAsync('HandleImagePaste', base64Str, item.type, targetId).catch(err => console.error(err));
                    };
                    reader.readAsDataURL(blob);
                    e.preventDefault();
                }
            }
        };
    }
};

window.downloadFile = (fileName, contentType, base64) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.getElementBounds = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return { top: 0, left: 0, bottom: 0, right: 0, width: 0, height: 0, viewportWidth: window.innerWidth, viewportHeight: window.innerHeight };
    const rect = el.getBoundingClientRect();
    return { top: rect.top, left: rect.left, bottom: rect.bottom, right: rect.right, width: rect.width, height: rect.height, viewportWidth: window.innerWidth, viewportHeight: window.innerHeight };
};

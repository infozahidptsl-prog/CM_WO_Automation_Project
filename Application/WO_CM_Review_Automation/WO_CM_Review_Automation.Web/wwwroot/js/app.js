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

async function renderElementToCanvas(element) {
    const hiddenElements = element.querySelectorAll('.print-hide');
    const originalDisplays = [];
    hiddenElements.forEach(el => {
        originalDisplays.push(el.style.display);
        el.style.display = 'none';
    });

    const prevOverflow = element.style.overflow;
    const prevWidth = element.style.width;
    const prevMaxWidth = element.style.maxWidth;
    element.style.overflow = 'visible';
    element.style.width = 'max-content';
    element.style.maxWidth = 'none';

    try {
        const canvas = await html2canvas(element, {
            scale: 2.5,
            useCORS: true,
            backgroundColor: '#ffffff',
            scrollX: 0,
            scrollY: 0,
            windowWidth: Math.max(document.documentElement.clientWidth, element.scrollWidth + 60),
            windowHeight: Math.max(document.documentElement.clientHeight, element.scrollHeight + 60)
        });
        return canvas;
    } finally {
        element.style.overflow = prevOverflow;
        element.style.width = prevWidth;
        element.style.maxWidth = prevMaxWidth;
        hiddenElements.forEach((el, index) => {
            el.style.display = originalDisplays[index];
        });
    }
}

async function captureAndDownload(element, fileName) {
    const canvas = await renderElementToCanvas(element);
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
        await new Promise((resolve) => {
            const script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js';
            script.onload = resolve;
            script.onerror = resolve;
            document.head.appendChild(script);
        });
    }
    return await captureAndCopy(element);
}

async function captureAndCopy(element) {
    try {
        // 1. Try Modern Async Clipboard API (passing Promise<Blob> directly into ClipboardItem so user gesture isn't lost)
        if (navigator.clipboard && navigator.clipboard.write && typeof window.ClipboardItem !== 'undefined') {
            try {
                const blobPromise = renderElementToCanvas(element).then(canvas => {
                    return new Promise((resolve, reject) => {
                        canvas.toBlob(b => b ? resolve(b) : reject(new Error('Blob empty')), 'image/png');
                    });
                });
                const item = new window.ClipboardItem({ 'image/png': blobPromise });
                await navigator.clipboard.write([item]);
                return true;
            } catch (clipErr) {
                console.warn('ClipboardItem(Promise) failed, falling back:', clipErr);
            }
        }

        // 2. Render canvas once for fallbacks
        const canvas = await renderElementToCanvas(element);

        // 2a. Try standard Blob ClipboardItem if Promise form wasn't supported
        if (navigator.clipboard && navigator.clipboard.write && typeof window.ClipboardItem !== 'undefined') {
            try {
                const blob = await new Promise(res => canvas.toBlob(res, 'image/png'));
                if (blob) {
                    await navigator.clipboard.write([new window.ClipboardItem({ 'image/png': blob })]);
                    return true;
                }
            } catch (blobErr) {
                console.warn('Standard ClipboardItem failed, trying DOM selection fallback:', blobErr);
            }
        }

        // 3. Fallback for HTTP / non-secure LAN IPs (e.g. http://10.192.168.78:5052) using contenteditable + execCommand('copy')
        const dataUrl = canvas.toDataURL('image/png');
        const holder = document.createElement('div');
        holder.contentEditable = 'true';
        holder.style.position = 'fixed';
        holder.style.left = '-99999px';
        holder.style.top = '0';
        holder.style.opacity = '0';

        const img = document.createElement('img');
        img.src = dataUrl;
        holder.appendChild(img);
        document.body.appendChild(holder);

        // Wait for image decode
        if (img.decode) {
            try { await img.decode(); } catch { }
        }

        const range = document.createRange();
        range.selectNode(img);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        const copied = document.execCommand('copy');
        selection.removeAllRanges();
        document.body.removeChild(holder);

        if (copied) {
            return true;
        }

        return false;
    } catch (err) {
        console.error('Failed to capture/copy image: ', err);
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

window.copyTextToClipboard = async function (text) {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
        const textArea = document.createElement("textarea");
        textArea.value = text;
        textArea.style.position = "fixed";
        textArea.style.left = "-9999px";
        textArea.style.top = "-9999px";
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        const ok = document.execCommand("copy");
        document.body.removeChild(textArea);
        return ok;
    } catch (err) {
        console.error("Failed to copy text:", err);
        return false;
    }
};

window.woAuthSession = {
    getState: function () {
        try {
            return localStorage.getItem("wo_cm_auth_token") || sessionStorage.getItem("wo_cm_auth_token") || "";
        } catch {
            return "";
        }
    },
    setState: function (username, remember) {
        try {
            if (remember) {
                localStorage.setItem("wo_cm_auth_token", username);
                sessionStorage.setItem("wo_cm_auth_token", username);
            } else {
                sessionStorage.setItem("wo_cm_auth_token", username);
                localStorage.removeItem("wo_cm_auth_token");
            }
        } catch { }
    },
    clearState: function () {
        try {
            localStorage.removeItem("wo_cm_auth_token");
            sessionStorage.removeItem("wo_cm_auth_token");
        } catch { }
    }
};

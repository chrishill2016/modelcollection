// Browser helpers used by BrowserInterop.cs.

export function watchConnectivity(dotnetRef) {
    const notify = () => dotnetRef.invokeMethodAsync('OnConnectivityChanged', navigator.onLine);
    window.addEventListener('online', notify);
    window.addEventListener('offline', notify);
    return navigator.onLine;
}

// Shrinks a photo to fit within maxSize px and returns { full, thumbnail } JPEG data URLs.
export async function resizeImage(streamRef, fullSize, thumbnailSize) {
    const blob = new Blob([await streamRef.arrayBuffer()]);
    const bitmap = await createImageBitmap(blob, { imageOrientation: 'from-image' });
    try {
        return {
            full: draw(bitmap, fullSize, 0.82),
            thumbnail: draw(bitmap, thumbnailSize, 0.75),
        };
    } finally {
        bitmap.close();
    }
}

function draw(bitmap, maxSize, quality) {
    const scale = Math.min(1, maxSize / Math.max(bitmap.width, bitmap.height));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(bitmap.width * scale);
    canvas.height = Math.round(bitmap.height * scale);
    canvas.getContext('2d').drawImage(bitmap, 0, 0, canvas.width, canvas.height);
    return canvas.toDataURL('image/jpeg', quality);
}

export function downloadText(fileName, contentType, text) {
    const url = URL.createObjectURL(new Blob([text], { type: contentType }));
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}

// Tells .NET when a new service worker has installed and is waiting to take over.
export async function watchForUpdates(dotnetRef) {
    if (!('serviceWorker' in navigator)) return;
    const registration = await navigator.serviceWorker.getRegistration();
    if (!registration) return;

    const notify = () => dotnetRef.invokeMethodAsync('OnUpdateAvailable');
    if (registration.waiting && navigator.serviceWorker.controller) notify();

    registration.addEventListener('updatefound', () => {
        const worker = registration.installing;
        worker?.addEventListener('statechange', () => {
            if (worker.state === 'installed' && navigator.serviceWorker.controller) notify();
        });
    });

    // Check for a new version every 30 minutes while the app is open.
    setInterval(() => registration.update().catch(() => { }), 30 * 60 * 1000);
}

export async function applyUpdate() {
    const registration = await navigator.serviceWorker.getRegistration();
    if (!registration?.waiting) {
        location.reload();
        return;
    }
    navigator.serviceWorker.addEventListener('controllerchange', () => location.reload(), { once: true });
    registration.waiting.postMessage({ type: 'SKIP_WAITING' });
}


export function goBack() {
    history.back();
}

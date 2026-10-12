// Minimal promise wrapper over IndexedDB used by JsIndexedDb.cs.
const DB_NAME = 'model-collection';
const DB_VERSION = 2;

let dbPromise;

function open() {
    dbPromise ??= new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains('cars')) db.createObjectStore('cars', { keyPath: 'id' });
            for (const name of ['photos', 'thumbnails']) {
                if (!db.objectStoreNames.contains(name)) {
                    db.createObjectStore(name, { keyPath: 'id' }).createIndex('carId', 'carId');
                }
            }
            if (!db.objectStoreNames.contains('outbox')) db.createObjectStore('outbox', { keyPath: 'carId' });
            if (!db.objectStoreNames.contains('meta')) db.createObjectStore('meta', { keyPath: 'key' });
            // v2: photo sync
            if (!db.objectStoreNames.contains('photoOutbox')) db.createObjectStore('photoOutbox', { keyPath: 'photoId' });
        };
        request.onsuccess = () => {
            const db = request.result;
            // Let a newer version of the app (in another tab) upgrade the database.
            db.onversionchange = () => { db.close(); dbPromise = undefined; };
            resolve(db);
        };
        request.onerror = () => reject(request.error);
    });
    return dbPromise;
}

async function run(storeName, mode, action) {
    const db = await open();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, mode);
        const request = action(tx.objectStore(storeName));
        tx.oncomplete = () => resolve(request.result);
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error);
    });
}

export const get = (store, key) => run(store, 'readonly', s => s.get(key)).then(v => v ?? null);
export const getAll = store => run(store, 'readonly', s => s.getAll());
export const getAllByIndex = (store, index, value) => run(store, 'readonly', s => s.index(index).getAll(value));
export const put = (store, value) => run(store, 'readwrite', s => s.put(value));
export const remove = (store, key) => run(store, 'readwrite', s => s.delete(key));
export const count = store => run(store, 'readonly', s => s.count());

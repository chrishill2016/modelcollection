// Firebase Auth + Firestore wrapper used by FirebaseAuthService.cs and FirestoreRemoteStore.cs.
// The SDK is vendored under lib/firebase so it is cached by the service worker and works offline.
import { initializeApp } from '../lib/firebase/firebase-app.js';
import {
    getAuth, GoogleAuthProvider, onAuthStateChanged, signInWithPopup, signInWithRedirect, signOut as firebaseSignOut,
} from '../lib/firebase/firebase-auth.js';
import {
    getFirestore, collection, doc, query, where, orderBy, limit, getDocs, setDoc, updateDoc, writeBatch,
    serverTimestamp, Timestamp,
} from '../lib/firebase/firebase-firestore-lite.js';
import { getStorage, ref, uploadString, getBlob, deleteObject } from '../lib/firebase/firebase-storage.js';

let auth;
let db;
let storage;

export function init(config, dotnetRef) {
    const app = initializeApp({
        apiKey: config.apiKey,
        authDomain: config.authDomain,
        projectId: config.projectId,
        appId: config.appId,
        storageBucket: config.storageBucket || undefined,
    });
    auth = getAuth(app);
    db = getFirestore(app);
    if (config.storageBucket) storage = getStorage(app);
    onAuthStateChanged(auth, user => {
        dotnetRef.invokeMethodAsync('OnAuthChanged', user && user.email ? {
            uid: user.uid,
            email: user.email,
            displayName: user.displayName,
            photoUrl: user.photoURL,
        } : null);
    });
}

export async function signIn() {
    const provider = new GoogleAuthProvider();
    provider.setCustomParameters({ prompt: 'select_account' });
    try {
        await signInWithPopup(auth, provider);
    } catch (e) {
        if (e?.code === 'auth/popup-blocked') {
            await signInWithRedirect(auth, provider);
        } else if (e?.code !== 'auth/popup-closed-by-user' && e?.code !== 'auth/cancelled-popup-request') {
            throw e;
        }
    }
}

export const signOut = () => firebaseSignOut(auth);

const toCollection = snap => ({ id: snap.id, name: snap.data().name, memberEmails: snap.data().memberEmails ?? [] });

export async function findCollection(email) {
    const q = query(collection(db, 'collections'), where('memberEmails', 'array-contains', email), limit(1));
    const result = await getDocs(q);
    return result.empty ? null : toCollection(result.docs[0]);
}

export async function createCollection(name, ownerEmail) {
    const ref = doc(collection(db, 'collections'));
    const data = { name, memberEmails: [ownerEmail], createdAt: serverTimestamp() };
    await setDoc(ref, data);
    return { id: ref.id, name, memberEmails: data.memberEmails };
}

export async function updateMembers(collectionId, memberEmails) {
    await updateDoc(doc(db, 'collections', collectionId), { memberEmails });
}

export async function pullCars(collectionId, cursor) {
    const cars = collection(db, 'collections', collectionId, 'cars');
    const q = query(cars, where('serverUpdatedAt', '>=', Timestamp.fromMillis(cursor)), orderBy('serverUpdatedAt'));
    const result = await getDocs(q);
    let next = cursor;
    const list = result.docs.map(d => {
        const { serverUpdatedAt, ...car } = d.data();
        if (serverUpdatedAt) next = Math.max(next, serverUpdatedAt.toMillis());
        return car;
    });
    return { cars: list, cursor: next };
}

export async function pushCars(collectionId, cars) {
    const batch = writeBatch(db);
    for (const car of cars) {
        batch.set(doc(db, 'collections', collectionId, 'cars', car.id), { ...car, serverUpdatedAt: serverTimestamp() });
    }
    await batch.commit();
}

// Photos: metadata in Firestore (collections/{id}/photos/{photoId}), images in Cloud Storage
// (collections/{id}/photos/{photoId}.jpg and collections/{id}/thumbs/{photoId}.jpg).

const photoPath = (collectionId, photoId, thumbnail) =>
    `collections/${collectionId}/${thumbnail ? 'thumbs' : 'photos'}/${photoId}.jpg`;

const imageMetadata = { contentType: 'image/jpeg', cacheControl: 'private, max-age=31536000, immutable' };

export async function pullPhotos(collectionId, cursor) {
    const photos = collection(db, 'collections', collectionId, 'photos');
    const q = query(photos, where('serverUpdatedAt', '>=', Timestamp.fromMillis(cursor)), orderBy('serverUpdatedAt'));
    const result = await getDocs(q);
    let next = cursor;
    const list = result.docs.map(d => {
        const { serverUpdatedAt, ...photo } = d.data();
        if (serverUpdatedAt) next = Math.max(next, serverUpdatedAt.toMillis());
        return photo;
    });
    return { photos: list, cursor: next };
}

export async function uploadPhoto(collectionId, photo, fullDataUrl, thumbnailDataUrl) {
    await uploadString(ref(storage, photoPath(collectionId, photo.id, true)), thumbnailDataUrl, 'data_url', imageMetadata);
    await uploadString(ref(storage, photoPath(collectionId, photo.id, false)), fullDataUrl, 'data_url', imageMetadata);
    // Write metadata last so other devices only see photos whose files exist.
    await setDoc(doc(db, 'collections', collectionId, 'photos', photo.id), { ...photo, serverUpdatedAt: serverTimestamp() });
}

export async function deletePhoto(collectionId, photoId, carId) {
    await setDoc(doc(db, 'collections', collectionId, 'photos', photoId), {
        id: photoId, carId, created: new Date().toISOString(), isDeleted: true, serverUpdatedAt: serverTimestamp(),
    });
    for (const thumbnail of [true, false]) {
        try {
            await deleteObject(ref(storage, photoPath(collectionId, photoId, thumbnail)));
        } catch (e) {
            if (e?.code !== 'storage/object-not-found') throw e;
        }
    }
}

export async function downloadPhoto(collectionId, photoId, thumbnail) {
    const blob = await getBlob(ref(storage, photoPath(collectionId, photoId, thumbnail)));
    return await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(blob);
    });
}

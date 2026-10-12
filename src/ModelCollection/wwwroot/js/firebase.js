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

let auth;
let db;

export function init(config, dotnetRef) {
    const app = initializeApp({
        apiKey: config.apiKey,
        authDomain: config.authDomain,
        projectId: config.projectId,
        appId: config.appId,
    });
    auth = getAuth(app);
    db = getFirestore(app);
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

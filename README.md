# Model Cars

An app for cataloguing a model car collection. It records each car (make, model, year, brand, scale, colour, condition, box, purchase price, value, where it's kept, notes and tags) and its photos. You can search and filter the collection, see stats, and export backups.

- **Works offline.** It's an installable PWA. Everything is saved to the device first (IndexedDB) and synced to the cloud when online.
- **Shared.** Sign in with Google to sync to Firebase Firestore. Everyone added to a collection can see and edit it.
- **Photos** are compressed and stored on the device that took them. They are included in JSON backups but are not synced (yet).

Built with Blazor WebAssembly (.NET 10), MudBlazor and Firebase. It's hosted on GitHub Pages.

## Run locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/ModelCollection   # app on http://localhost:5048
dotnet test                                # unit (xUnit) and component (bUnit) tests
```

Without Firebase config the app runs local-only. To sync locally, create `src/ModelCollection/wwwroot/appsettings.Development.json` (git-ignored):

```json
{ "Firebase": { "ApiKey": "...", "AuthDomain": "<project>.firebaseapp.com", "ProjectId": "<project>", "AppId": "..." } }
```

and add `localhost` to Firebase Auth → Settings → Authorised domains (it usually is by default).

## Deployment

- **Pull requests to `main`:** restore, build and test (`.github/workflows/ci.yml`).
- **Merges to `main`:** build, test, publish and deploy to GitHub Pages (`.github/workflows/deploy.yml`). The version is `<Version>` major.minor from the csproj plus the run number, and is shown in the app footer.
- The Pages fix-ups are in `.github/scripts/prepare-pages.sh`: the `<base href>` is set to `/<repo>/`, the service-worker hash is refreshed, `404.html` is created for deep links, and `.nojekyll` is added.

### One-time setup

1. **GitHub Pages:** Repo → Settings → Pages → Source: **GitHub Actions**.
2. **Firebase project** (free Spark plan is fine):
   1. Create a project at <https://console.firebase.google.com> and add a **Web app**. Copy its config.
   2. **Authentication** → Sign-in method → enable **Google**. Under Settings → Authorised domains, add `<your-github-username>.github.io`.
   3. **Firestore Database** → Create database (production mode). Then, under **Rules**, paste in [`firestore.rules`](firestore.rules) and publish.
3. **Repository variables:** Repo → Settings → Secrets and variables → Actions → **Variables**. Add `FIREBASE_API_KEY`, `FIREBASE_AUTH_DOMAIN`, `FIREBASE_PROJECT_ID` and `FIREBASE_APP_ID`.
   These values are public identifiers, not secrets. Access is enforced by the Firestore rules.
4. Push to `main`. Open the site, sign in, go to **Settings** and create the collection. Then add the other person's Google email so they can share it.

## How it works

```
src/ModelCollection/
  Features/            Collection (grid/list, filters), CarDetail (view/edit/photos), Stats, Backup, Settings
  Services/            ILocalStore (IndexedDB + outbox), ISyncService, IRemoteStore (Firestore),
                       IAuthService (Google via Firebase), ICollectionService (sharing), photos, backup
  Models/              ModelCar and friends
  wwwroot/js/          IndexedDB, Firebase and browser interop
  wwwroot/lib/firebase Vendored Firebase JS SDK 12.19.0 (Apache-2.0), so it is cached for offline use
tests/ModelCollection.Tests/   xUnit + bUnit
```

**Sync:**
- Every edit is written to IndexedDB and queued in an outbox.
- The sync service pulls cloud changes since its last cursor, then pushes the outbox.
- Sync runs on app start, when the connection comes back, after each local edit, every 5 minutes, and from **Sync now** in the status indicator.
- Conflicts are **last-write-wins** on the car's `LastModified` UTC timestamp.
- Deletes are soft (`IsDeleted`) so they sync too.

Firestore layout: `collections/{id}` holds `{ name, memberEmails[] }`, and cars are stored in `collections/{id}/cars/{carId}`.

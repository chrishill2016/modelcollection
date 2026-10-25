# Future ideas

Features talked about but not built yet. Nothing here is planned until it's asked for.

## Keeping data safe
- **Recently deleted, with Restore.** Deleted cars and photos stay in a "Recently deleted" page for about 30 days before they're removed for good, plus an **Undo** on the "Car deleted" message. Today a deleted car is only hidden (`isDeleted: true` in Firestore) with no way back in the app, and photos are removed from Storage straight away.
- **Backup back in Settings**, with an occasional reminder. The Backup page still works at `/backup` but is hidden from the menu.
- **Insurance report**: a printable PDF of every car with its photo, condition and value.
- **Who added or changed each car**: record an "added by" / "last edited by" email on each car.

## Getting cars in faster
- **Import an existing list** from a spreadsheet or Word file, or from photos of the printed book.
- **"Next needing details" button** on the edit screen, to work through quick-added cars one after another.
- **Bulk edit**: select several cars and set the brand, scale, series, tag or location for all of them.
- **Duplicate warning** when adding a car that's already in the collection.
- **Identify a car from a photo**: Claude suggests make, model, manufacturer and scale for confirmation. Needs a small Firebase Function to keep the AI key off the device.
- **Barcode scan** to fill in details from the box.

## Collector extras
- **Status**: owned, on loan, for sale, or sold, with sale price and profit or loss in Stats.
- **Value history**, showing how the collection's total value changes over time.
- **Browse by location**: what's in each cabinet or on each shelf.
- **Wishlist** of cars he's looking for.
- **Catalogue page** laid out like the printed book (road and racing cars, with Registration, Race no. and Driver fields). Built in PR #10 and closed unmerged when it wasn't wanted; the PR can be reopened.

## Sharing and ease of use
- **Read-only share link** so friends can browse without editing.
- **Larger text option** in Settings → Appearance.

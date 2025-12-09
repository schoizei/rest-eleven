const SHELL_CACHE = 'resteleven-shell-dev';
const API_CACHE = 'resteleven-api-dev';
const SHELL_URLS = ['/', '/index.html', '/manifest.json'];
let reminderConfig = null;

self.addEventListener('install', (event) => {
	event.waitUntil((async () => {
		const cache = await caches.open(SHELL_CACHE);
		await cache.addAll(SHELL_URLS);
		await self.skipWaiting();
	})());
});

self.addEventListener('activate', (event) => {
	event.waitUntil((async () => {
		const keys = await caches.keys();
		await Promise.all(keys.filter((key) => key.startsWith('resteleven-') && key !== SHELL_CACHE && key !== API_CACHE).map((key) => caches.delete(key)));
		await self.clients.claim();
	})());
});

self.addEventListener('fetch', (event) => {
	const { request } = event;
	if (request.method !== 'GET') {
		return;
	}

	const url = new URL(request.url);
	if (request.mode === 'navigate' || SHELL_URLS.includes(url.pathname)) {
		event.respondWith(cacheFirst(request, SHELL_CACHE));
		return;
	}

	if (url.pathname.startsWith('/push') || url.pathname.startsWith('/personio') || url.pathname.startsWith('/api/')) {
		event.respondWith(networkFirst(request));
		return;
	}

	event.respondWith(cacheFirst(request, SHELL_CACHE));
});

self.addEventListener('push', (event) => {
	const data = event.data ? event.data.json() : {};
	const title = data.title || 'RestEleven';
	const body = data.body || 'Neue Erinnerung verfügbar';
	const notificationOptions = {
		body,
		icon: '/icons/icon-192.png',
		badge: '/icons/icon-192.png',
		data: { url: data.url || '/' }
	};

	event.waitUntil(self.registration.showNotification(title, notificationOptions));
});

self.addEventListener('notificationclick', (event) => {
	event.notification.close();
	const url = event.notification.data?.url || '/';
	event.waitUntil(clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
		for (const client of clientList) {
			if ('focus' in client) {
				return client.focus();
			}
		}

		return clients.openWindow(url);
	}));
});

self.addEventListener('message', (event) => {
	if (event.data?.type === 'resteleven-reminder') {
		reminderConfig = event.data.payload;
	}
});

self.addEventListener('periodicsync', (event) => {
	if (event.tag !== 'resteleven-daily-reminder') {
		return;
	}

	event.waitUntil(triggerReminder());
});

async function cacheFirst(request, cacheName) {
	const cache = await caches.open(cacheName);
	const cached = await cache.match(request);
	if (cached) {
		return cached;
	}

	const response = await fetch(request);
	cache.put(request, response.clone());
	return response;
}

async function networkFirst(request) {
	try {
		const response = await fetch(request);
		const cache = await caches.open(API_CACHE);
		cache.put(request, response.clone());
		return response;
	} catch (error) {
		const cache = await caches.open(API_CACHE);
		const cached = await cache.match(request);
		if (cached) {
			return cached;
		}
		throw error;
	}
}

async function triggerReminder() {
	if (!reminderConfig) {
		return;
	}

	const now = new Date();
	const target = new Date(now);
	target.setHours(reminderConfig.hour, reminderConfig.minute, 0, 0);

	const difference = Math.abs(now - target);
	if (difference <= 30 * 60 * 1000) {
		await self.registration.showNotification(reminderConfig.title, {
			body: reminderConfig.body,
			icon: '/icons/icon-192.png',
			data: { url: reminderConfig.url || '/' }
		});
	}
}

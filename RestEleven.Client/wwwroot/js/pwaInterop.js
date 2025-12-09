window.resteleven = window.resteleven || {};

const urlBase64ToUint8Array = (base64String) => {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; i++) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
};

resteleven.notifications = {
    ensurePermission: async () => {
        if (!('Notification' in window)) {
            return 'denied';
        }

        if (Notification.permission === 'granted') {
            return 'granted';
        }

        if (Notification.permission !== 'denied') {
            return await Notification.requestPermission();
        }

        return Notification.permission;
    },
    subscribe: async (publicKey) => {
        const registration = await navigator.serviceWorker.ready;
        const existing = await registration.pushManager.getSubscription();
        if (existing) {
            return {
                endpoint: existing.endpoint,
                keys: existing.toJSON().keys
            };
        }

        const subscription = await registration.pushManager.subscribe({
            applicationServerKey: urlBase64ToUint8Array(publicKey),
            userVisibleOnly: true
        });

        return {
            endpoint: subscription.endpoint,
            keys: subscription.toJSON().keys
        };
    },
    show: async ({ title, body, url }) => {
        const registration = await navigator.serviceWorker.ready;
        if (registration.showNotification) {
            await registration.showNotification(title, {
                body,
                icon: '/icons/icon-192.png',
                data: { url }
            });
        } else {
            new Notification(title, { body });
        }
    },
    scheduleReminder: async ({ hour, minute, title, body, url }) => {
        const registration = await navigator.serviceWorker.ready;
        registration.active?.postMessage({
            type: 'resteleven-reminder',
            payload: { hour, minute, title, body, url }
        });
    }
};

resteleven.storage = {
    get: (key) => window.localStorage.getItem(key),
    set: (key, value) => window.localStorage.setItem(key, value)
};

resteleven.sync = {
    supported: async () => {
        if (!('serviceWorker' in navigator)) {
            return false;
        }

        const registration = await navigator.serviceWorker.ready;
        return 'periodicSync' in registration;
    },
    register: async (tag) => {
        const registration = await navigator.serviceWorker.ready;
        if (!('periodicSync' in registration)) {
            return;
        }

        await registration.periodicSync.register(tag, {
            minInterval: 24 * 60 * 60 * 1000
        });
    }
};

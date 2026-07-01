/** Stable per-browser identity used to key the persistent profile on the server. */
export function getDeviceId(): string {
  const KEY = "dream_device_id";
  let id = localStorage.getItem(KEY);
  if (!id || !/^[a-zA-Z0-9_-]{8,64}$/.test(id)) {
    id = `d${Date.now().toString(36)}${Math.random().toString(36).slice(2, 12)}`;
    localStorage.setItem(KEY, id);
  }
  return id;
}

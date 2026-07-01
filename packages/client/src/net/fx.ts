type Listener = (data: Record<string, unknown>) => void;

class FxBus {
  private listeners = new Map<string, Set<Listener>>();

  on(type: string, cb: Listener): () => void {
    let set = this.listeners.get(type);
    if (!set) {
      set = new Set();
      this.listeners.set(type, set);
    }
    set.add(cb);
    return () => set!.delete(cb);
  }

  emit(type: string, data: Record<string, unknown>) {
    this.listeners.get(type)?.forEach((cb) => cb(data));
    this.listeners.get("*")?.forEach((cb) => cb({ type, ...data }));
  }
}

export const fxBus = new FxBus();

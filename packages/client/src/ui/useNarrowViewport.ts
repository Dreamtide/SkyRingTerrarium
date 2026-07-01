import { useEffect, useState } from "react";

/** True when the viewport is narrow enough that stacked HUD panels need a compact layout. */
export function useNarrowViewport(breakpoint = 640): boolean {
  const [narrow, setNarrow] = useState(() => window.innerWidth < breakpoint);
  useEffect(() => {
    const onResize = () => setNarrow(window.innerWidth < breakpoint);
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, [breakpoint]);
  return narrow;
}

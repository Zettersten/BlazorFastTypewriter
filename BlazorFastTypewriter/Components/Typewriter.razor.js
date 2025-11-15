export function checkReducedMotion() {
  if (typeof window === 'undefined' || !window.matchMedia) {
    return false;
  }

  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

export async function waitForElement(containerId, timeoutMs = 2000) {
  if (typeof document === 'undefined') {
    return false;
  }

  const startTime = Date.now();
  const selector = `[data-typewriter-id="${containerId}"]`;

  while (Date.now() - startTime < timeoutMs) {
    const element = document.querySelector(selector);
    if (element && element instanceof HTMLElement && element.childNodes.length > 0) {
      return true;
    }
    await new Promise(resolve => setTimeout(resolve, 50));
  }

  return false;
}

export function extractStructure(containerId) {
  if (typeof document === 'undefined') {
    return { nodes: [] };
  }

  const element = document.querySelector(`[data-typewriter-id="${containerId}"]`);

  if (!element || !(element instanceof HTMLElement) || element.childNodes.length === 0) {
    return { nodes: [] };
  }
  
  const textContent = element.textContent?.trim() || '';
  if (textContent.length === 0) {
    return { nodes: [] };
  }

  const processNode = (node) => {
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.textContent;
      if (!text || !/\S/.test(text)) {
        return null;
      }

      return {
        type: 'text',
        text
      };
    }

    if (node.nodeType === Node.ELEMENT_NODE) {
      const element = /** @type {Element} */ (node);
      const tagName = element.tagName.toLowerCase();
      const attributes = Object.create(null);

      for (const { name, value } of element.attributes) {
        attributes[name] = value;
      }

      const children = Array.from(element.childNodes)
        .map(processNode)
        .filter(Boolean);

      return {
        type: 'element',
        tagName,
        attributes: Object.keys(attributes).length > 0 ? attributes : undefined,
        children: children.length > 0 ? children : undefined
      };
    }

    return null;
  };

  const nodes = Array.from(element.childNodes)
    .map(processNode)
    .filter(Boolean);

  return { nodes };
}

export function dispose() {
}

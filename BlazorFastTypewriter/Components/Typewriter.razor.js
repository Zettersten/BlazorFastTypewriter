/**
 * JavaScript module for Typewriter component.
 * Handles DOM structure extraction and reduced motion detection.
 * Optimized for SSR, Blazor Server, and WASM compatibility.
 */

/**
 * Checks if the user prefers reduced motion.
 * @returns {boolean} True if prefers-reduced-motion is enabled.
 */
export function checkReducedMotion() {
  // SSR compatibility check
  if (typeof window === 'undefined' || !window.matchMedia) {
    return false;
  }

  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * Waits for an element to be available in the DOM.
 * @param {string} containerId - The data attribute ID to find the container element.
 * @param {number} timeoutMs - Maximum time to wait in milliseconds (default: 2000).
 * @returns {Promise<boolean>} True if element is found and has content, false if timeout.
 */
export async function waitForElement(containerId, timeoutMs = 2000) {
  // SSR compatibility check
  if (typeof document === 'undefined') {
    return false;
  }

  const startTime = Date.now();
  const selector = `[data-typewriter-id="${containerId}"]`;

  while (Date.now() - startTime < timeoutMs) {
    const element = document.querySelector(selector);
    if (element && element instanceof HTMLElement) {
      // Also check if element has content (child nodes)
      if (element.childNodes.length > 0) {
        return true;
      }
    }
    // Wait a bit before checking again
    await new Promise(resolve => setTimeout(resolve, 50));
  }

  return false;
}

/**
 * Extracts the DOM structure from an element, flattening it into a tree structure.
 * @param {string} containerId - The data attribute ID to find the container element.
 * @returns {Object} The extracted DOM structure.
 */
export function extractStructure(containerId) {
  // SSR compatibility check
  if (typeof document === 'undefined') {
    return { nodes: [] };
  }

  const element = document.querySelector(`[data-typewriter-id="${containerId}"]`);

  if (!element || !(element instanceof HTMLElement)) {
    return { nodes: [] };
  }

  /**
   * Recursively processes a DOM node and converts it to our structure format.
   * Uses modern JS patterns and optimizations.
   * @param {Node} node - The DOM node to process.
   * @returns {Object|null} The processed node structure or null if skipped.
   */
  const processNode = (node) => {
    // Fast path: text nodes
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.textContent;

      // Skip empty or whitespace-only text nodes using regex test
      if (!text || !/\S/.test(text)) {
        return null;
      }

      return {
        type: 'text',
        text
      };
    }

    // Fast path: element nodes
    if (node.nodeType === Node.ELEMENT_NODE) {
      const element = /** @type {Element} */ (node);
      const tagName = element.tagName.toLowerCase();

      // Use Object.create(null) for better performance (no prototype chain)
      const attributes = Object.create(null);

      // Modern iteration over attributes
      for (const { name, value } of element.attributes) {
        attributes[name] = value;
      }

      // Process children recursively with filter to remove nulls
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

  // Process all child nodes and filter out nulls
  const nodes = Array.from(element.childNodes)
    .map(processNode)
    .filter(Boolean);

  return { nodes };
}

/**
 * Disposes any resources held by this module.
 * Called when the component is disposed.
 */
export function dispose() {
  // No resources to dispose in current implementation
  // This is here for future extensibility and best practices
}

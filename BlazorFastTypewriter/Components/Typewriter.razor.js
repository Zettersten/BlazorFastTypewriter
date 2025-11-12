/**
 * JavaScript module for Typewriter component.
 * Handles DOM structure extraction and reduced motion detection.
 */

/**
 * Checks if the user prefers reduced motion.
 * @returns {boolean} True if prefers-reduced-motion is enabled.
 */
export function checkReducedMotion() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * Extracts the DOM structure from an element, flattening it into a tree structure.
 * @param {string} containerId - The data attribute ID to find the container element.
 * @returns {Object} The extracted DOM structure.
 */
export function extractStructure(containerId) {
    const element = document.querySelector(`[data-typewriter-id="${containerId}"]`);
    
    if (!element || !(element instanceof HTMLElement)) {
        return { nodes: [] };
    }

    /**
     * Recursively processes a DOM node and converts it to our structure format.
     * @param {Node} node - The DOM node to process.
     * @returns {Object|null} The processed node structure or null if skipped.
     */
    function processNode(node) {
        if (node.nodeType === Node.TEXT_NODE) {
            const text = node.textContent;
            if (!text || !/\S/.test(text)) {
                return null; // Skip whitespace-only text nodes
            }
            return {
                type: 'text',
                text: text
            };
        }

        if (node.nodeType === Node.ELEMENT_NODE) {
            const element = /** @type {Element} */ (node);
            const tagName = element.tagName.toLowerCase();
            const attributes = {};
            
            // Extract attributes
            for (const attr of element.attributes) {
                attributes[attr.name] = attr.value;
            }

            const children = [];
            for (const child of element.childNodes) {
                const processed = processNode(child);
                if (processed !== null) {
                    children.push(processed);
                }
            }

            return {
                type: 'element',
                tagName: tagName,
                attributes: attributes,
                children: children.length > 0 ? children : undefined
            };
        }

        return null;
    }

    const nodes = [];
    for (const child of element.childNodes) {
        const processed = processNode(child);
        if (processed !== null) {
            nodes.push(processed);
        }
    }

    return { nodes: nodes };
}

// Initialize syntax highlighting for code blocks
window.highlightCodeBlocks = function(element) {
    if (typeof hljs === 'undefined') {
        console.warn('Highlight.js not loaded');
        return;
    }
    
    const codeBlocks = element.querySelectorAll('pre code');
    codeBlocks.forEach((block) => {
        // Auto-detect language or use manual class
        if (!block.classList.contains('hljs')) {
            hljs.highlightElement(block);
        }
    });
};

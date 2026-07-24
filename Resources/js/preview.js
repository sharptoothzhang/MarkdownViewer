function scrollToHeading(anchor) {
    var element = document.getElementById(anchor);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

function setOutline(visible) {
    var t = document.getElementById('outline-toggle'), o = document.getElementById('outline-nav');
    if (!t || !o) return;
    var hide = (visible !== 'visible'), now = o.classList.contains('collapsed');
    if (hide !== now) {
        o.classList.toggle('collapsed');
        t.textContent = hide ? '▶' : '◀';
        try { window.chrome.webview.postMessage('OUTLINE:' + (hide ? 'hidden' : 'visible')); } catch(e) {}
    }
}
function toggleOutline() {
    var t = document.getElementById('outline-toggle'), o = document.getElementById('outline-nav');
    if (!t || !o) return;
    var c = o.classList.toggle('collapsed');
    t.textContent = c ? '▶' : '◀';
    try { window.chrome.webview.postMessage('OUTLINE:' + (c ? 'hidden' : 'visible')); } catch(e) {}
}

document.addEventListener('DOMContentLoaded', function() {
    var outline = document.getElementById('outline-nav');
    if (outline && typeof HEADINGS !== 'undefined' && HEADINGS.length > 0) {
        var h = '<div class="outline-header">目录</div><div class="outline-list">';
        for (var i = 0; i < HEADINGS.length; i++) {
            var n = HEADINGS[i], pad = 12 + (n.level - 1) * 16;
            var esc = n.text.replace(/"/g,'&quot;').replace(/'/g,'&#39;');
            h += '<div class="outline-item level-' + n.level + '" style="padding-left:' + pad + 'px" title="' + esc + '" onclick=\'scrollToHeading("' + n.anchor + '")\'>' + esc + '</div>';
        }
        h += '</div>';
        outline.innerHTML = h;
    }

    try {
        if (typeof hljs !== 'undefined') {
            document.querySelectorAll('pre code').forEach(function(block) {
                hljs.highlightElement(block);
            });
        }
    } catch(ex) {}

    var mn = document.querySelectorAll('.mermaid[data-b64]');
    if (mn.length > 0) {
        for (var i = 0; i < mn.length; i++) {
            try {
                var b64 = mn[i].getAttribute('data-b64');
                var text = decodeURIComponent(escape(atob(b64)));
                mn[i].textContent = text;
            } catch(e) {
                window.chrome.webview.postMessage('MERMAID_DECODE_ERR:' + e.message);
            }
        }
        var s = document.createElement('script');
        s.src = 'https://appassets.local/Resources/js/mermaid.min.js';
        s.onload = function() {
            mermaid.initialize({ startOnLoad: false, theme: 'default', securityLevel: 'loose' });
            if (typeof mermaid.run === 'function') {
                mermaid.run({ nodes: document.querySelectorAll('.mermaid') }).then(function() {
                    window.chrome.webview.postMessage('MERMAID_OK');
                }).catch(function(e) {
                    window.chrome.webview.postMessage('MERMAID_ERR:' + e.message);
                });
            } else if (typeof mermaid.init === 'function') {
                mermaid.init({ nodes: document.querySelectorAll('.mermaid') });
                window.chrome.webview.postMessage('MERMAID_OK');
            } else {
                window.chrome.webview.postMessage('MERMAID_ERR:no render function');
            }
        };
        document.head.appendChild(s);
    }

    document.getElementById('outline-toggle').addEventListener('click', toggleOutline);

    window.chrome.webview.postMessage('RENDERED');
});

document.addEventListener('keydown', function(e) {
    if (e.ctrlKey) {
        var key = e.key.toUpperCase();
        if (key === 'N' || key === 'O' || key === 'P' || key === 'E' || key === 'S') {
            e.preventDefault();
            window.chrome.webview.postMessage('KEY_' + key);
        }
    }
});

window.onerror = function(msg, url, line) {
    try { window.chrome.webview.postMessage('JS_ERROR:' + msg); } catch(e) {}
    return true;
};

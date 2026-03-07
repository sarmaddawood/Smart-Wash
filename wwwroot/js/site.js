const progressStyle = document.createElement('style');
progressStyle.textContent = `
    #sw-progress-bar {
        position: fixed;
        top: 0;
        left: 0;
        height: 3px;
        background: var(--sw-primary);
        z-index: 9999;
        transition: width 0.4s ease, opacity 0.4s ease;
        box-shadow: 0 0 10px rgba(37, 99, 235, 0.5);
        pointer-events: none;
        opacity: 0;
        width: 0%;
    }
`;
document.head.appendChild(progressStyle);

const progressBar = document.createElement('div');
progressBar.id = 'sw-progress-bar';
document.body.appendChild(progressBar);

let progressInterval;

function startProgress() {
    progressBar.style.opacity = '1';
    progressBar.style.width = '10%';
    let width = 10;

    clearInterval(progressInterval);
    progressInterval = setInterval(() => {
        if (width >= 90) {
            clearInterval(progressInterval);
        } else {
            width += Math.random() * 10;
            progressBar.style.width = width + '%';
        }
    }, 200);
}

function completeProgress() {
    clearInterval(progressInterval);
    progressBar.style.width = '100%';
    setTimeout(() => {
        progressBar.style.opacity = '0';
        setTimeout(() => {
            progressBar.style.width = '0%';
        }, 400);
    }, 300);
}

document.addEventListener('DOMContentLoaded', () => {
    completeProgress();

    document.body.addEventListener('click', (e) => {
        const target = e.target.closest('a');
        if (target && target.href && !target.href.startsWith('#') && target.href !== window.location.href && !target.hasAttribute('download') && target.target !== '_blank') {
            startProgress();

            const pageWrapper = document.querySelector('.page-wrapper');
            if (pageWrapper) {
                pageWrapper.classList.add('page-exit');
            }
        }
    });

    // Handle form submissions
    document.body.addEventListener('submit', (e) => {
        startProgress();
        const pageWrapper = document.querySelector('.page-wrapper');
        if (pageWrapper && !e.target.hasAttribute('data-no-exit')) {
            pageWrapper.classList.add('page-exit');
        }
    });
});

window.addEventListener('pageshow', (e) => {
    if (e.persisted) {
        completeProgress();
        const pageWrapper = document.querySelector('.page-wrapper');
        if (pageWrapper) {
            pageWrapper.classList.remove('page-exit');
        }
    }
});

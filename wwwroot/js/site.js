// wwwroot/js/site.js

document.addEventListener('DOMContentLoaded', function () {
    // Предпросмотр изображений
    let darkmode = localStorage.getItem('darkmode');
    //console.log(darkmode, darkmode != null, darkmode == 1)
    if (darkmode != null & darkmode == 0) {
        document.getElementById('themeToggle').querySelector('i').className = 'fas fa-moon';
        document.body.classList.toggle('dark');
    }
    const fileInputs = document.querySelectorAll('input[type="file"]');
    fileInputs.forEach(input => {
        input.addEventListener('change', function () {
            const previewId = this.getAttribute('data-preview');
            if (previewId) {
                previewFile(this, previewId);
            }
        });
    });

    // Плавное появление контента
    const mainContent = document.querySelector('main');
    if (mainContent) {
        mainContent.classList.add('fade-in');
    }
});

function previewFile(input, previewId) {
    const preview = document.getElementById(previewId);
    const file = input.files[0];

    if (file) {
        const reader = new FileReader();

        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.style.display = 'block';
            preview.parentElement.style.display = 'block';
        }

        reader.readAsDataURL(file);
    }
}

function validateFileSize(input, maxSizeMB) {
    if (input.files.length > 0) {
        const fileSize = input.files[0].size / 1024 / 1024;
        if (fileSize > maxSizeMB) {
            alert(`Файл слишком большой. Максимальный размер: ${maxSizeMB}MB`);
            input.value = '';
            return false;
        }
    }
    return true;
}

function showLoading(progressBarId) {
    const progressBar = document.getElementById(progressBarId);
    if (progressBar) {
        let progress = 0;
        const interval = setInterval(() => {
            progress += 5;
            progressBar.style.width = Math.min(progress, 90) + '%';

            if (progress >= 90) {
                clearInterval(interval);
            }
        }, 300);
    }
}

// Для форм загрузки файлов
document.querySelectorAll('form').forEach(form => {
    form.addEventListener('submit', function (e) {
        const fileInput = this.querySelector('input[type="file"]');
        const loadingElement = this.querySelector('#loading');

        if (fileInput && (!fileInput.files || !fileInput.files[0])) {
            e.preventDefault();
            alert('Пожалуйста, выберите файл для анализа');
            return;
        }

        if (loadingElement) {
            loadingElement.style.display = 'block';
            showLoading('progressFill');
        }
    });
});
(() => {
    const input = document.getElementById('DocumentosPdf');
    const container = document.getElementById('pdf-selection');
    if (!input || !container || typeof DataTransfer === 'undefined') return;
    const list = document.getElementById('pdf-selection-list');
    const status = document.getElementById('pdf-selection-status');
    const error = document.getElementById('pdf-selection-error');
    const maxFile = 10 * 1024 * 1024;
    const maxTotal = 50 * 1024 * 1024;
    let selected = [];
    const size = bytes => bytes < 1024 * 1024
        ? `${(bytes / 1024).toLocaleString('es', { maximumFractionDigits: 1 })} KB`
        : `${(bytes / 1024 / 1024).toLocaleString('es', { maximumFractionDigits: 2 })} MB`;
    const render = () => {
        const transfer = new DataTransfer();
        selected.forEach(file => transfer.items.add(file));
        input.files = transfer.files;
        list.replaceChildren();
        container.hidden = selected.length === 0;
        status.textContent = `${selected.length} archivo(s) seleccionado(s) · ${size(selected.reduce((sum, f) => sum + f.size, 0))} de 50 MB`;
        selected.forEach((file, index) => {
            const item = document.createElement('li');
            item.className = 'list-group-item d-flex flex-wrap justify-content-between align-items-center gap-2';
            const name = document.createElement('span');
            name.className = 'text-break';
            name.textContent = `${file.name} · ${size(file.size)}`;
            const remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'btn btn-outline-danger btn-sm';
            remove.textContent = 'Quitar';
            remove.setAttribute('aria-label', `Quitar ${file.name}`);
            remove.addEventListener('click', () => {
                selected.splice(index, 1);
                error.textContent = '';
                render();
                const buttons = list.querySelectorAll('button');
                (buttons[Math.min(index, buttons.length - 1)] || input).focus();
            });
            item.append(name, remove);
            list.append(item);
        });
    };
    input.addEventListener('change', () => {
        const errors = [];
        for (const file of input.files) {
            if (!/\.pdf$/i.test(file.name) || file.size === 0 || file.size > maxFile) {
                errors.push(`${file.name}: debe ser un PDF con contenido y de hasta 10 MB.`);
                continue;
            }
            if (selected.some(f => f.name === file.name && f.size === file.size && f.lastModified === file.lastModified)) continue;
            if (selected.reduce((sum, f) => sum + f.size, 0) + file.size > maxTotal) {
                errors.push(`${file.name}: supera los 50 MB de esta carga. Puede añadirlo después de guardar el borrador.`);
                continue;
            }
            selected.push(file);
        }
        error.textContent = errors.join(' ');
        render();
    });
})();

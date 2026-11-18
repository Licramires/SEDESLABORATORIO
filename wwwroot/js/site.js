// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('submit', event => {
    const prompt = event.submitter?.dataset.confirm || event.target.dataset.confirm;
    if (prompt && !window.confirm(prompt)) event.preventDefault();
});

// Fonction pour télécharger un fichier
globalThis.downloadFile = function (filename, content, contentType) {
    const blob = new Blob([content], { type: contentType });
    const url = globalThis.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    globalThis.URL.revokeObjectURL(url);
};

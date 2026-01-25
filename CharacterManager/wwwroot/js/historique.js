// Fonction pour télécharger un fichier
// - Si contentType est fourni, on considère que le contenu est déjà brut (ex: JSON)
// - Sinon, on considère que le contenu est une chaîne base64
globalThis.downloadFile = function (filename, content, contentType) {
    let link = document.createElement('a');

    if (contentType) {
        const blob = new Blob([content], { type: contentType });
        const url = globalThis.URL.createObjectURL(blob);
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
        globalThis.URL.revokeObjectURL(url);
    } else {
        link.download = filename;
        link.href = 'data:application/octet-stream;base64,' + content;
        document.body.appendChild(link);
        link.click();
        link.remove();
    }
};

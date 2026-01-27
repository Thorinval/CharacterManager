// Fonction pour télécharger un fichier
// - Si contentType est fourni, on considère que le contenu est déjà brut (ex: JSON)
// - Sinon, on considère que le contenu est une chaîne base64
globalThis.downloadFile = function (filename, content, contentType) {
    try {
        console.log(`[downloadFile] Démarrage du téléchargement: ${filename}`);
        console.log(`[downloadFile] Type: ${contentType || 'base64'}`);
        console.log(`[downloadFile] Taille du contenu: ${content.length} caractères`);
        
        if (contentType) {
            // Créer un blob avec le contenu
            const blob = new Blob([content], { type: contentType });
            console.log(`[downloadFile] Taille du blob: ${blob.size} octets (${(blob.size / 1024).toFixed(2)} KB)`);
            
            // Vérifier si le blob est trop gros pour certaines méthodes
            if (blob.size > 100 * 1024 * 1024) { // > 100 MB
                console.warn(`[downloadFile] ATTENTION: Fichier volumineux (${(blob.size / 1024 / 1024).toFixed(2)} MB)`);
            }
            
            // Créer une URL pour le blob
            const url = globalThis.URL.createObjectURL(blob);
            console.log(`[downloadFile] URL blob créée: ${url.substring(0, 50)}...`);
            
            // Créer un élément <a> et déclencher le téléchargement
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            link.style.display = 'none';
            
            document.body.appendChild(link);
            console.log(`[downloadFile] Élément <a> ajouté au DOM`);
            
            // Déclencher le téléchargement
            console.log(`[downloadFile] Déclenchement du clic...`);
            link.click();
            console.log(`[downloadFile] Clic déclenché`);
            
            // Nettoyer après un court délai pour s'assurer que le téléchargement a démarré
            setTimeout(() => {
                document.body.removeChild(link);
                globalThis.URL.revokeObjectURL(url);
                console.log(`[downloadFile] Nettoyage terminé`);
            }, 1000); // Augmenté à 1 seconde pour les gros fichiers
        } else {
            // Mode base64 - pour les petits fichiers uniquement
            console.log(`[downloadFile] Mode base64`);
            const link = document.createElement('a');
            link.download = filename;
            link.href = 'data:application/octet-stream;base64,' + content;
            link.style.display = 'none';
            
            document.body.appendChild(link);
            link.click();
            
            setTimeout(() => {
                document.body.removeChild(link);
                console.log(`[downloadFile] Nettoyage base64 terminé`);
            }, 100);
        }
        
        console.log(`[downloadFile] Téléchargement déclenché avec succès`);
        return true;
    } catch (error) {
        console.error(`[downloadFile] ERREUR:`, error);
        console.error(`[downloadFile] Type d'erreur: ${error.name}`);
        console.error(`[downloadFile] Message: ${error.message}`);
        console.error(`[downloadFile] Stack:`, error.stack);
        throw error;
    }
};


// Charger Chart.js depuis CDN si ce n'est pas déjà fait
let chartJsLoaded = false;

async function loadChartJs() {
    if (chartJsLoaded) return;
    
    return new Promise((resolve, reject) => {
        if (typeof Chart !== 'undefined') {
            chartJsLoaded = true;
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js';
        script.onload = () => {
            chartJsLoaded = true;
            resolve();
        };
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

// Créer un graphique camembert
export async function createPieChart(canvasId, labels, data, colors) {
    try {
        // Charger Chart.js si nécessaire
        await loadChartJs();

        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error(`Canvas avec l'id ${canvasId} non trouvé`);
            return;
        }

        // Détruire le graphique existant s'il y en a un
        const existingChart = Chart.getChart(ctx);
        if (existingChart) {
            existingChart.destroy();
        }

        // Créer le nouveau graphique
        new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderColor: '#fff',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 15,
                            font: {
                                size: 14
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((value / total) * 100).toFixed(1);
                                return `${label}: ${value} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Erreur lors de la création du graphique:', error);
    }
}

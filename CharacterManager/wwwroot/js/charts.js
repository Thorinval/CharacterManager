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
                    maintainAspectRatio: false,
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

// Créer un graphique en ligne pour l'évolution des niveaux
export async function createLineChart(canvasId, labels, datasets, options = {}) {
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

        // Injecter le style CSS pour la légende en colonnes si nécessaire
        if (!document.getElementById('chart-legend-style')) {
            const style = document.createElement('style');
            style.id = 'chart-legend-style';
            style.textContent = `
                .chart-legend-columns {
                    display: flex;
                    flex-wrap: wrap;
                    max-width: 300px;
                }
                .chart-legend-columns > * {
                    flex: 0 0 25%;
                    margin-bottom: 8px;
                }
            `;
            document.head.appendChild(style);
        }

        // Créer le nouveau graphique
        const chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: false,
                        min: options.minLevel === undefined ? 0 : options.minLevel,
                        title: {
                            display: true,
                            text: 'Niveau'
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Date'
                        },
                        ticks: {
                            callback: function(value, index) {
                                // Afficher le label pour chaque point si graduations quotidiennes
                                if (options.showDayNumbers) {
                                    return labels[index];
                                }
                                return labels[index];
                            },
                            maxRotation: 45,
                            minRotation: 0
                        }
                    }
                }
            }
        });

        // Appliquer le style en colonnes à la légende
        const legendCanvas = chart.legend?.ctx?.canvas;
        if (legendCanvas) {
            const canvasParent = legendCanvas.parentElement;
            if (canvasParent) {
                const legendContainer = canvasParent.querySelector('[role="region"][aria-label*="legend"]') 
                    || canvasParent.querySelector('ul');
                if (legendContainer) {
                    legendContainer.classList.add('chart-legend-columns');
                    // Forcer 4 colonnes avec flexbox
                    legendContainer.style.display = 'flex';
                    legendContainer.style.flexWrap = 'wrap';
                    legendContainer.style.maxWidth = '300px';
                    
                    const items = legendContainer.querySelectorAll('li');
                    items.forEach(item => {
                        item.style.flex = '0 0 25%';
                        item.style.marginBottom = '8px';
                    });
                }
            }
        }
    } catch (error) {
        console.error('Erreur lors de la création du graphique de ligne:', error);
    }
}

// Créer un graphique en barres
export async function createBarChart(canvasId, labels, datasets, options = {}) {
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

        // Créer le nouveau graphique en barres
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'top',
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
                                const label = context.dataset.label || '';
                                const value = context.parsed.y || 0;
                                return `${label}: ${value}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: 'Date'
                        },
                        ticks: {
                            maxRotation: 45,
                            minRotation: 0
                        }
                    },
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Rang'
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Erreur lors de la création du graphique en barres:', error);
    }
}

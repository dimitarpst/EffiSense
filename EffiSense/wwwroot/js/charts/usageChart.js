function getCssVariable(variableName) {
    try {
        return getComputedStyle(document.documentElement).getPropertyValue(variableName).trim();
    } catch (e) {
        console.error("Error getting CSS variable:", variableName, e);
        return null;
    }
}

function addAlpha(color, opacity) {
    if (!color || color === '') return `rgba(200, 200, 200, ${opacity})`;
    if (color.startsWith('#')) {
        if (color.length === 7) {
            let r = parseInt(color.slice(1, 3), 16);
            let g = parseInt(color.slice(3, 5), 16);
            let b = parseInt(color.slice(5, 7), 16);
            return `rgba(${r}, ${g}, ${b}, ${opacity})`;
        } else if (color.length === 4) {
            let r = parseInt(color.slice(1, 2) + color.slice(1, 2), 16);
            let g = parseInt(color.slice(2, 3) + color.slice(2, 3), 16);
            let b = parseInt(color.slice(3, 4) + color.slice(3, 4), 16);
            return `rgba(${r}, ${g}, ${b}, ${opacity})`;
        }
    }
    if (color.startsWith('rgb')) {
        return color.replace(/rgb(a?)\s*\(([\d,\s.]+)\)/, `rgba($2, ${opacity})`);
    }
    return color;
}


document.addEventListener("DOMContentLoaded", function () {

    const textColorPrimary = getCssVariable('--text-primary') || '#d6eac1';
    const textColorSecondary = getCssVariable('--text-secondary') || '#8cac81';
    const borderColor = getCssVariable('--border-color') || '#547c5c';
    const gridColor = addAlpha(borderColor, 0.15);
    const tooltipBg = getCssVariable('--bg-tertiary') || '#3c6c3c';

    const primaryAccent = getCssVariable('--accent-primary') || '#8cac81';
    const secondaryAccent = getCssVariable('--accent-secondary') || '#537b44';
    const lightAccent = getCssVariable('--accent-light-bg') || '#d6eac1';
    const mediumGreen = getCssVariable('--green-medium') || '#507b41';
    const desaturatedGreen = getCssVariable('--green-desaturated') || '#547c5c';
    const greyishGreen = getCssVariable('--green-greyish') || '#71926c';


    const chartColorPalette = [
        primaryAccent,
        secondaryAccent,
        greyishGreen,
        desaturatedGreen,
        mediumGreen,
        textColorSecondary
    ];
    const chartColorPaletteLight = [
        addAlpha(primaryAccent, 0.7),
        addAlpha(secondaryAccent, 0.7),
        addAlpha(greyishGreen, 0.7),
        addAlpha(desaturatedGreen, 0.7),
        addAlpha(mediumGreen, 0.7),
        addAlpha(textColorSecondary, 0.7)
    ];

    Chart.defaults.color = textColorSecondary;
    Chart.defaults.borderColor = gridColor;
    Chart.defaults.plugins.legend.labels.color = textColorPrimary;
    Chart.defaults.plugins.tooltip.backgroundColor = tooltipBg;
    Chart.defaults.plugins.tooltip.titleColor = textColorPrimary;
    Chart.defaults.plugins.tooltip.bodyColor = textColorSecondary;
    Chart.defaults.plugins.tooltip.borderColor = borderColor;
    Chart.defaults.plugins.tooltip.borderWidth = 1;

    let charts = [];

    function createChart(ctx, type, labels, data, label, backgroundColor, borderColor, options = {}) {
        const defaultOptions = {
            responsive: true,
            maintainAspectRatio: false,
            animation: {
                duration: 500,
                easing: 'easeOutQuad'
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { color: textColorSecondary },
                    grid: { color: gridColor }
                },
                x: {
                    ticks: { color: textColorSecondary },
                    grid: { color: gridColor }
                },
                r: {
                    ticks: {
                        color: textColorSecondary,
                        backdropColor: addAlpha(getCssVariable('--bg-secondary') || '#2f563c', 0.75)
                    },
                    grid: { color: gridColor },
                    angleLines: { color: gridColor },
                    pointLabels: { color: textColorPrimary }
                }
            },
            plugins: {
                legend: {
                    display: type !== 'doughnut' && type !== 'pie' && type !== 'polarArea',
                    position: "top",
                    labels: { color: textColorPrimary }
                },
                tooltip: {}
            }
        };

        const mergedOptions = { ...defaultOptions, ...options };
        if (options.scales) {
            mergedOptions.scales = { ...defaultOptions.scales, ...options.scales };
            if (options.scales.y) mergedOptions.scales.y = { ...defaultOptions.scales.y, ...options.scales.y };
            if (options.scales.x) mergedOptions.scales.x = { ...defaultOptions.scales.x, ...options.scales.x };
            if (options.scales.r) mergedOptions.scales.r = { ...defaultOptions.scales.r, ...options.scales.r };
        }
        if (options.plugins) {
            mergedOptions.plugins = { ...defaultOptions.plugins, ...options.plugins };
            if (options.plugins.legend) mergedOptions.plugins.legend = { ...defaultOptions.plugins.legend, ...options.plugins.legend };
            if (options.plugins.tooltip) mergedOptions.plugins.tooltip = { ...defaultOptions.plugins.tooltip, ...options.plugins.tooltip };
        }

        let chart = new Chart(ctx, {
            type: type,
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    backgroundColor: backgroundColor || chartColorPaletteLight,
                    borderColor: borderColor || chartColorPalette,
                    borderWidth: type === 'line' ? 2 : 1,
                    tension: type === 'line' ? 0.3 : 0,
                    fill: type === 'line' ? { target: 'origin', above: addAlpha(primaryAccent, 0.1) } : false,
                    hoverOffset: 4
                }]
            },
            options: mergedOptions
        });

        charts.push(chart);
        return chart;
    }

    function fetchDataAndCreateChart(endpoint, canvasId, type, label, defaultBgColor, defaultBorderColor, extraOptions = {}) {
        const canvasElement = document.getElementById(canvasId);
        if (!canvasElement) {
            console.error(`Canvas element with ID "${canvasId}" not found.`);
            return;
        }
        const ctx = canvasElement.getContext("2d");

        fetch(endpoint)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                const chartLabels = data.labels || data.daysOfWeek || data.months || data.buildingTypes || data.applianceNames || data.homeNames || data.hours || [];
                const chartData = data.energyUsed || [];

                if (chartLabels.length === 0 || chartData.length === 0) {
                    console.warn(`No data found for chart "${canvasId}" at endpoint "${endpoint}".`);
                    ctx.font = `14px ${getCssVariable('font-family') || 'sans-serif'}`;
                    ctx.fillStyle = textColorSecondary;
                    ctx.textAlign = 'center';
                    ctx.fillText('No data available', canvasElement.width / 2, canvasElement.height / 2);
                    return;
                }

                const bgColor = defaultBgColor || addAlpha(primaryAccent, 0.5);
                const bdColor = defaultBorderColor || primaryAccent;

                const multiBgColor = Array.isArray(defaultBgColor) ? defaultBgColor : chartColorPaletteLight;
                const multiBdColor = Array.isArray(defaultBorderColor) ? defaultBorderColor : chartColorPalette;

                let finalBgColor, finalBdColor;
                if (type === 'doughnut' || type === 'pie' || type === 'polarArea') {
                    finalBgColor = multiBgColor;
                    finalBdColor = multiBdColor;
                } else {
                    finalBgColor = bgColor;
                    finalBdColor = bdColor;
                }

                createChart(ctx, type, chartLabels, chartData, label, finalBgColor, finalBdColor, extraOptions);
            })
            .catch(error => {
                console.error(`Error fetching data for chart "${canvasId}":`, error);
                const canvasElement = document.getElementById(canvasId);
                if (canvasElement) {
                    const ctx = canvasElement.getContext("2d");
                    ctx.font = `14px ${getCssVariable('font-family') || 'sans-serif'}`;
                    ctx.fillStyle = getCssVariable('--danger-color') || '#e57373';
                    ctx.textAlign = 'center';
                    ctx.fillText('Error loading chart data.', canvasElement.width / 2, canvasElement.height / 2);
                }
            });
    }

    fetchDataAndCreateChart('/Home/GetUsageData', "usageChart", 'bar',
        'Daily Usage (kWh)', addAlpha(primaryAccent, 0.6), primaryAccent);

    fetchDataAndCreateChart('/Home/GetApplianceData', "applianceChart", 'doughnut',
        'Usage by Appliance (kWh)', chartColorPalette, chartColorPalette);

    fetchDataAndCreateChart('/Home/GetHomeData', "homeChart", 'bar',
        'Usage by Home (kWh)', addAlpha(secondaryAccent, 0.6), secondaryAccent);

    fetchDataAndCreateChart('/Home/GetPeakTimeData', "peakTimeChart", 'line',
        'Usage by Hour of Day (kWh)', addAlpha(lightAccent, 0.5), lightAccent);

    fetchDataAndCreateChart('/Home/GetPeakTimeData', "hourlyChart", 'bar',
        'Hourly Usage (kWh)', addAlpha(greyishGreen, 0.7), greyishGreen, {
        plugins: { legend: { display: false } }
    });

    fetchDataAndCreateChart('/Home/GetDayOfWeekData', "weekdayChart", 'polarArea',
        'Usage by Weekday', chartColorPalette, chartColorPalette, {});


    const sidebarToggle = document.getElementById("sidebarToggle");
    if (sidebarToggle) {
        sidebarToggle.addEventListener("click", function () {
            setTimeout(() => {
                charts.forEach(chart => {
                    if (chart && typeof chart.resize === 'function') {
                        chart.resize();
                    }
                });
            }, 350);
        });
    } else {
        console.warn("Sidebar toggle button not found.");
    }

});
document.addEventListener("DOMContentLoaded", function () {
    let charts = [];

    function createChart(ctx, type, labels, data, label, backgroundColor, borderColor, extraOptions = {}) {
        let chart = new Chart(ctx, {
            type: type,
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    backgroundColor: backgroundColor,
                    borderColor: borderColor,
                    borderWidth: 2,
                    tension: 0.4, // Smooth curves for line charts
                    fill: true // Fill effect for area charts
                }]
            },
            options: Object.assign({
                responsive: true,
                maintainAspectRatio: false,
                devicePixelRatio: 2,
                scales: {
                    y: { beginAtZero: true }
                },
                animation: { duration: 800 },
                plugins: {
                    legend: { display: true, position: "top" }
                }
            }, extraOptions)
        });

        charts.push(chart);
        return chart;
    }

    function fetchDataAndCreateChart(endpoint, ctx, type, label, bgColor, borderColor, extraOptions = {}) {
        fetch(endpoint)
            .then(response => response.json())
            .then(data => {
                createChart(ctx, type, data.labels || data.applianceNames || data.homeNames || data.hours,
                    data.energyUsed, label, bgColor, borderColor, extraOptions);
            });
    }

    fetchDataAndCreateChart('/Home/GetUsageData', document.getElementById("usageChart").getContext("2d"), 'bar',
        'Energy Used (kWh)', 'rgba(75, 192, 192, 0.2)', 'rgba(75, 192, 192, 1)');

    fetchDataAndCreateChart('/Home/GetApplianceData', document.getElementById("applianceChart").getContext("2d"), 'doughnut',
        'Energy Usage (kWh)', ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF'], '');

    fetchDataAndCreateChart('/Home/GetHomeData', document.getElementById("homeChart").getContext("2d"), 'bar',
        'Energy Used (kWh)', 'rgba(153, 102, 255, 0.2)', 'rgba(153, 102, 255, 1)');

    fetchDataAndCreateChart('/Home/GetPeakTimeData', document.getElementById("peakTimeChart").getContext("2d"), 'line',
        'Energy Used (kWh)', 'rgba(255, 99, 132, 1)', 'rgba(255, 99, 132, 1)');

    fetchDataAndCreateChart('/Home/GetPeakTimeData', document.getElementById("hourlyChart").getContext("2d"), 'bar',
        'Hourly Usage (kWh)', 'rgba(255, 159, 64, 0.6)', 'rgba(255, 159, 64, 1)', {
        scales: { y: { grid: { color: "rgba(255, 159, 64, 0.2)" } } }
    });

    fetchDataAndCreateChart('/Home/GetDayOfWeekData', document.getElementById("weekdayChart").getContext("2d"), 'polarArea',
        'Usage by Weekday', ['#ff6384', '#ff9f40', '#ffcd56', '#4bc0c0', '#36a2eb', '#9966ff', '#c9cbcf'], '', {
        scales: { r: { angleLines: { display: false } } }
    });


    document.getElementById("sidebarToggle").addEventListener("click", function () {
        requestAnimationFrame(() => {
            charts.forEach(chart => chart.resize());
        });
    });

});

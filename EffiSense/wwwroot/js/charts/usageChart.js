document.addEventListener("DOMContentLoaded", function () {
    // Energy Usage by Day (Bar Chart)
    const usageCtx = document.getElementById("usageChart").getContext("2d");
    fetch('/Home/GetUsageData')
        .then(response => response.json())
        .then(data => {
            new Chart(usageCtx, {
                type: 'bar',
                data: {
                    labels: data.labels,
                    datasets: [{
                        label: 'Energy Used (kWh)',
                        data: data.energyUsed,
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        borderColor: 'rgba(75, 192, 192, 1)',
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                }
            });
        });

    // Energy Usage by Appliance (Doughnut Chart)
    const applianceCtx = document.getElementById("applianceChart").getContext("2d");
    fetch('/Home/GetApplianceData')
        .then(response => response.json())
        .then(data => {
            new Chart(applianceCtx, {
                type: 'doughnut',
                data: {
                    labels: data.applianceNames,
                    datasets: [{
                        label: 'Energy Usage (kWh)',
                        data: data.energyUsed,
                        backgroundColor: ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF'],
                        hoverOffset: 4
                    }]
                },
                options: {
                    responsive: true
                }
            });
        });

   
});

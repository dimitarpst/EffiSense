document.addEventListener("DOMContentLoaded", function () {
    const ctx = document.getElementById("usageChart").getContext("2d");

    fetch('/Home/GetUsageData') 
        .then(response => response.json())
        .then(data => {
            new Chart(ctx, {
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
        })
        .catch(error => console.error('Error fetching chart data:', error));
});

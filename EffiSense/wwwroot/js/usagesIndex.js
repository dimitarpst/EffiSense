$(document).ready(function () {
    $("#filterDate").change(function () {
        const selectedDate = $(this).val();
        if (selectedDate) {
            localStorage.setItem("selectedDate", selectedDate);
        } else {
            localStorage.removeItem("selectedDate");
        }

        $("#usageCardContainer").fadeOut(200, function () {
            $.ajax({
                url: '/Usages/FilterByDate',
                data: { date: selectedDate },
                success: function (data) {
                    $("#usageCardContainer").html(data).fadeIn(300);
                }
            });
        });
    });

    const storedDate = localStorage.getItem("selectedDate");
    if (storedDate) {
        $("#filterDate").val(storedDate);
    }
});
$(document).ready(function () {
    const usageContainer = $("#usageCardContainer");

    if (usageContainer.length) {
        console.log("📡 SignalR UI update enabled for Usages/Index.cshtml");

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/usageHub")
            .build();

        connection.start()
            .then(() => console.log("📡 SignalR Connected"))
            .catch(err => console.error("SignalR Error:", err));

        connection.on("ReceiveUsageUpdate", function (usage) {

            console.log("📡 New usage received:", usage);

            const newUsageCard = `
                <div class="col-xl-4 col-lg-6 col-md-6 col-12 usage-entry">
                    <div class="card border-0 shadow">
                        <div class="card-body">
                            <h3 class="card-title text-dark">${usage.applianceName}</h3>
                            <ul class="list-unstyled mt-3 mb-4">
                                <li><strong>House Name:</strong> ${usage.homeName}</li>
                                <li><strong>Date:</strong> ${usage.date}</li>
                                <li><strong>Energy Used:</strong> ${usage.energyUsed} kWh</li>
                                <li><strong>Frequency:</strong> ${getUsageFrequencyText(usage.usageFrequency)}</li>
                            </ul>
                            <div class="row g-2">
                                <div class="col-4">
                                    <a href="/Usages/Edit/${usage.usageId}" class="btn btn-warning btn-lg w-100">Edit</a>
                                </div>
                                <div class="col-4">
                                    <a href="/Usages/Details/${usage.usageId}" class="btn btn-info btn-lg w-100">Details</a>
                                </div>
                                <div class="col-4">
                                    <a href="/Usages/Delete/${usage.usageId}" class="btn btn-danger btn-lg w-100">Delete</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            const newElement = $(newUsageCard).hide().fadeIn(500);
            usageContainer.append(newElement);
        });

        function getUsageFrequencyText(frequency) {
            const usageFrequencyMap = {
                1: "Rarely",
                2: "Sometimes",
                3: "Often",
                4: "Very Often",
                5: "Always"
            };
            return usageFrequencyMap[frequency] || "Unknown";
        }
    }
});


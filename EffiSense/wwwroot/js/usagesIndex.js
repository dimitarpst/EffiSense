$(document).ready(function () {
    const filterInput = $("#filterDate");
    const masonryGridContainer = $("#usagesGridContainer .masonry-grid-container");

    const $filter = $('#filterDate');
    const $container = $('#usagesGridContainer');

    if (!$filter.length) return;  // bail if the date picker isn't there

    function applyDateFilter(rawDate) {
        const date = rawDate ? rawDate.substring(0, 10) : '';
        console.log('🔍 Filtering usages for date:', date);

        if (date) localStorage.setItem('selectedDate', date);
        else localStorage.removeItem('selectedDate');

        $container.css('opacity', 0.5);
        $.ajax({
            url: '/Usages/FilterByDate',
            data: { dateFilter: date },
            success: function (html) {
                $container
                    .html(html)
                    .css('opacity', 1)
                    .hide()
                    .fadeIn(300);
            },
            error: function (xhr, status, err) {
                console.error('Error filtering usages:', status, err);
                $container
                    .html('<p class="text-center">Error loading data.</p>')
                    .css('opacity', 1);
            }
        });
    }

    $filter.on('change', function () {
        applyDateFilter(this.value);
    });

    const saved = localStorage.getItem('selectedDate');
    if (saved) {
        $filter.val(saved);
        applyDateFilter(saved);
    }

    filterInput.on('change', function () {
        applyDateFilter($(this).val());
    });

    const storedDate = localStorage.getItem("selectedDate");
    if (storedDate) {
        filterInput.val(storedDate);
        applyDateFilter(storedDate);
    }

    if (masonryGridContainer.length) {
        console.log("📡 SignalR UI update enabled for Usages Index.");

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/usageHub")
            .configureLogging(signalR.LogLevel.Information)
            .build();

        function getUsageFrequencyText(frequency) {
            const usageFrequencyMap = { 1: "Rarely", 2: "Sometimes", 3: "Often", 4: "Very Often", 5: "Always" };
            return usageFrequencyMap[frequency] || "N/A";
        }

        function createUsageCardHtml(usage) {
            let iconClass = usage.iconClass || 'fas fa-bolt';
            let displayDate = "N/A";
            let displayTime = "";
            if (usage.date) {
                try {
                    const dateObj = new Date(usage.date);
                    displayDate = dateObj.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
                    displayTime = dateObj.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
                } catch (e) { displayDate = usage.date; }
            }
            const applianceName = usage.applianceName || 'N/A';
            const homeName = usage.homeName || '';
            const energyUsed = usage.energyUsed?.toFixed(2) ?? 'N/A';
            const frequencyText = getUsageFrequencyText(usage.usageFrequency);
            const contextNotes = usage.contextNotes || '';

            return `
                <div class="card shadow">
                    <div class="card-body">

                        <div class="card-top-right-details">
                            <span class="card-efficiency-badge" title="Usage Frequency">
                                <i class="fas fa-sync-alt me-1"></i>${frequencyText}
                            </span>
                            ${usage.iconClass ? `<span class="card-efficiency-badge" title="Context Icon"><i class="${usage.iconClass}"></i></span>` : ''}
                        </div>

                        <div class="card-header-flex">
                            <div class="card-icon-container">
                                <i class="${iconClass} card-icon"></i>
                            </div>
                            <div class="card-title-section">
                                <div class="card-title-brand-group">
                                    <h5 class="card-main-title">${applianceName}</h5>
                                    ${homeName ? `<span class="card-brand-separator"></span><span class="card-brand-text">${homeName}</span>` : ''}
                                </div>
                                <p class="card-home-info-header">${displayDate} at ${displayTime}</p>
                            </div>
                        </div>

                        <div class="card-power-details">
                            <span class="card-power-value">${energyUsed}</span>
                            <span class="card-power-label">kWh</span>
                        </div>

                        ${contextNotes ? `<p class="card-note-text">${contextNotes}</p>` : '<div class="card-note-text-spacer"></div>'}

                        <div class="actions-row row g-2">
                            <div class="col">
                                <a href="/Usages/Edit/${usage.usageId}" class="btn btn-sm btn-warning w-100">Edit</a>
                            </div>
                            <div class="col">
                                <a href="/Usages/Details/${usage.usageId}" class="btn btn-sm btn-info w-100">Details</a>
                            </div>
                            <div class="col">
                                <a href="/Usages/Delete/${usage.usageId}" class="btn btn-sm btn-danger w-100">Delete</a>
                            </div>
                        </div>

                    </div>
                </div>`;
        }

        connection.on("ReceiveUsageUpdate", function (usage) {
            console.log("📡 New usage received:", usage);

            const currentFilter = filterInput.val();
            let matchesFilter = true;
            if (currentFilter && usage.date) {
                try {
                    const usageDateOnly = usage.date.substring(0, 10);
                    if (usageDateOnly !== currentFilter) {
                        matchesFilter = false;
                        console.log(`📡 Usage ${usage.usageId} (${usageDateOnly}) does not match filter ${currentFilter}. Skipping UI add.`);
                    }
                } catch (e) { console.error("Error comparing date filter", e); }
            }

            if (matchesFilter) {
                const newElement = $(createUsageCardHtml(usage)).hide();
                const targetContainer = $("#usagesGridContainer .masonry-grid-container");
                if (targetContainer.length) {
                    targetContainer.prepend(newElement);
                    newElement.fadeIn(500);
                } else {
                    console.error("Masonry grid container not found for SignalR update.");
                }
            }
        });

        async function startSignalR() {
            try {
                await connection.start();
                console.log("📡 SignalR Connected successfully.");
            } catch (err) {
                console.error("SignalR Connection Error: ", err);
            }
        }

        startSignalR();

        connection.onclose(async (error) => {
            console.warn("SignalR connection closed.", error);
        });

    } else {
        console.log("📡 SignalR UI update disabled (container not found on initial load).");
    }
});

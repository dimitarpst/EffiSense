function setupInfiniteScroll(gridContainerSelector, triggerSelector, loadingIndicatorSelector, masonryInstance = null) {
    const gridContainer = document.querySelector(gridContainerSelector);
    const $gridContainer = $(gridContainer);
    const $loadMoreTrigger = $(triggerSelector);
    const $loadingIndicator = $(loadingIndicatorSelector);

    if (!$gridContainer.length || !$loadMoreTrigger.length || !$loadingIndicator.length) {
        console.warn("Infinite scroll setup failed: Missing required jQuery elements.", { gridContainerSelector, triggerSelector, loadingIndicatorSelector });
        return;
    }

    let currentPage = parseInt($gridContainer.data('current-page') || '1');
    let pageSize = parseInt($gridContainer.data('page-size') || '9');
    let hasMoreItems = ($gridContainer.data('has-more-items') === true || $gridContainer.data('has-more-items') === 'true');
    const loadMoreUrl = $gridContainer.data('load-url');
    let isLoading = false;

    if (!loadMoreUrl) {
        console.error("Infinite scroll setup failed: Missing data-load-url attribute on container:", gridContainerSelector);
        $loadMoreTrigger.hide();
        return;
    }

    if (!hasMoreItems) {
        $loadMoreTrigger.hide();
        $loadingIndicator.hide();
    }

    const observer = new IntersectionObserver(entries => {
        const firstEntry = entries[0];
        if (firstEntry.isIntersecting && hasMoreItems && !isLoading) {
            loadNextPage();
        }
    }, { threshold: 0.1 });

    if (hasMoreItems) {
        observer.observe($loadMoreTrigger[0]);
    }

    function loadNextPage() {
        if (isLoading || !hasMoreItems) return;

        isLoading = true;
        $loadingIndicator.show();
        currentPage++;

        const url = `${loadMoreUrl}?pageNumber=${currentPage}`;
        console.log(`InfiniteScroll: Loading page ${currentPage} from ${url}`);

        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                if (html && html.trim().length > 0) {
                    const $tempContainer = $('<div></div>').html(html.trim());
                    const $newItems = $tempContainer.children('.card'); 

                    if ($newItems.length > 0) {
                        $newItems.css('opacity', 0);

                        $gridContainer.append($newItems);

                        if (masonryInstance) {
                            masonryInstance.appended($newItems.get()); 
                            masonryInstance.layout();
                        } else {
                            console.warn("Masonry instance not provided to infinite scroll.");
                        }

                        setTimeout(() => {
                            $newItems.animate({ opacity: 1 }, 500);
                        }, 100); 

                        if ($newItems.length < pageSize) {
                            console.log(`InfiniteScroll: Reached end (received ${$newItems.length} items).`);
                            hasMoreItems = false;
                            $loadMoreTrigger.hide();
                            observer.unobserve($loadMoreTrigger[0]);
                        } else {
                            $gridContainer.data('has-more-items', 'true');
                        }
                    } else {
                        console.log(`InfiniteScroll: Reached end (no card elements found in response).`);
                        hasMoreItems = false;
                        $loadMoreTrigger.hide();
                        observer.unobserve($loadMoreTrigger[0]);
                    }
                } else {
                    console.log(`InfiniteScroll: Reached end (empty response from server).`);
                    hasMoreItems = false;
                    $loadMoreTrigger.hide();
                    observer.unobserve($loadMoreTrigger[0]);
                }
            },
            error: function (xhr, status, error) {
                console.error('InfiniteScroll: Error loading more items:', status, error);
                hasMoreItems = false;
                $loadMoreTrigger.hide();
                observer.unobserve($loadMoreTrigger[0]);
            },
            complete: function () {
                isLoading = false;
                $loadingIndicator.hide();
            }
        });
    }
}
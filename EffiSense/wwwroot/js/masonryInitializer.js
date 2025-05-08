function initMasonryWithInfiniteScroll(
    gridWrapperSelector,
    gridContainerSelector,
    itemSelector,
    infiniteScrollTriggerSelector,
    infiniteScrollLoadingIndicatorSelector
) {
    const gridWrapper = document.querySelector(gridWrapperSelector);

    if (!gridWrapper) {
        console.warn(`Masonry Initializer: Wrapper element "${gridWrapperSelector}" not found. Cannot initialize masonry or infinite scroll.`);
        return;
    }

    const gridElement = gridWrapper.querySelector(gridContainerSelector);

    if (!gridElement) {
        console.warn(`Masonry Initializer: Grid container "${gridContainerSelector}" not found within "${gridWrapperSelector}". Masonry not initialized.`);
        return;
    }

    //console.log(`Masonry Initializer: Grid element found for "${gridWrapperSelector}". Waiting for images...`);

    imagesLoaded(gridElement, function () {
        //console.log(`Masonry Initializer: Images loaded for grid in "${gridWrapperSelector}". Initializing Masonry.`);

        const msnry = new Masonry(gridElement, {
            itemSelector: itemSelector, 
            columnWidth: itemSelector,  
            gutter: 0,                 
            percentPosition: true,     
            initLayout: true
        });

        //console.log(`Masonry Initializer: Masonry initialized for grid in "${gridWrapperSelector}".`, msnry);

        const loadMoreUrl = $(gridWrapper).data('load-url'); 

        if (typeof setupInfiniteScroll === 'function' &&
            loadMoreUrl &&
            infiniteScrollTriggerSelector &&
            infiniteScrollLoadingIndicatorSelector) {

            const triggerElement = document.querySelector(infiniteScrollTriggerSelector);
            const loadingIndicatorElement = document.querySelector(infiniteScrollLoadingIndicatorSelector);

            if (triggerElement && loadingIndicatorElement) {
                //console.log(`Masonry Initializer: Attempting to set up infinite scroll for "${gridWrapperSelector}".`);
                const fullGridContainerSelector = `${gridWrapperSelector} ${gridContainerSelector}`;

                setupInfiniteScroll(
                    fullGridContainerSelector,        
                    infiniteScrollTriggerSelector,   
                    infiniteScrollLoadingIndicatorSelector,
                    msnry                            
                );
                //console.log(`Masonry Initializer: Infinite scroll setup initiated for "${gridWrapperSelector}".`);
            } else {
                if (!triggerElement) console.warn(`Masonry Initializer: Infinite scroll trigger "${infiniteScrollTriggerSelector}" not found for "${gridWrapperSelector}". Infinite scroll NOT activated.`);
                if (!loadingIndicatorElement) console.warn(`Masonry Initializer: Infinite scroll loading indicator "${infiniteScrollLoadingIndicatorSelector}" not found for "${gridWrapperSelector}". Infinite scroll NOT activated.`);
            }
        } else {
            if (typeof setupInfiniteScroll !== 'function') {
                // console.log(`Masonry Initializer: setupInfiniteScroll function not found. Infinite scroll not configured for "${gridWrapperSelector}".`);
            }
            if (!loadMoreUrl && infiniteScrollTriggerSelector) { 
                console.warn(`Masonry Initializer: data-load-url attribute not found on "${gridWrapperSelector}". Infinite scroll cannot be activated.`);
            }
        }
    });
}

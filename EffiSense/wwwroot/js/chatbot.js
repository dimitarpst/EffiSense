$(function () {
    const $chatbotFab = $('#chatbot-fab');
    const $chatWindow = $('#chatbot-window');
    const $closeBtn = $('#chatbot-close-btn');
    const $sendBtn = $('#chatbot-send-btn');
    const $chatInput = $('#chatbot-input');
    const $chatBody = $('#chat-body');
    let isChatOpen = false;
    let typingIndicatorTimeout;
    let wordAppendTimeout;
    let historyLoadedOrLoading = false;

    // --- Function to Load Chat History ---
    function loadChatHistory() {
        if (historyLoadedOrLoading) return;

        historyLoadedOrLoading = true;
        $chatBody.empty();

        $.ajax({
            url: '/Usages/GetChatHistory',
            type: 'GET',
            dataType: 'json',
            success: function (messages) {
                if (messages && messages.length > 0) {
                    messages.forEach(function (msg) {
                        appendMessage(msg.text, msg.senderType === 'user' ? 'user-message' : 'bot-message', true);
                    });
                } else {
                    appendMessage("Hello! I'm your EffiSense assistant. How can I help you today?", 'bot-message', true);
                }
                // Use timeout to ensure elements are rendered before scrolling instantly
                setTimeout(() => scrollToBottom(true), 0);
            },
            error: function (xhr, status, error) {
                console.error("Error loading chat history:", status, error, xhr.responseText);
                appendMessage("Hello! I'm your EffiSense assistant. How can I help you today?", 'bot-message', true);
                setTimeout(() => scrollToBottom(true), 0);
            }
        });
    }

    // --- Event Listeners ---
    $chatbotFab.on('click', toggleChatWindow);
    $closeBtn.on('click', toggleChatWindow);

    function toggleChatWindow() {
        isChatOpen = !isChatOpen;
        $chatWindow.toggleClass('open');
        $chatbotFab.toggleClass('fab-open');

        if (isChatOpen) {
            historyLoadedOrLoading = false;
            loadChatHistory();
            setTimeout(() => $chatInput.focus(), 50);
        } else {
            clearTimeout(wordAppendTimeout);
        }
    }

    $sendBtn.on('click', sendMessage);
    $chatInput.on('keypress', function (e) {
        if (e.key === 'Enter' && $chatInput.val().trim() !== '') {
            e.preventDefault();
            sendMessage();
        }
    });

    // --- Send Message Logic ---
    function sendMessage() {
        const messageText = $chatInput.val().trim();
        if (messageText === '') {
            return;
        }

        appendMessage(messageText, 'user-message', true);
        $chatInput.val('');
        clearTimeout(wordAppendTimeout);

        const showIndicatorDelay = 150;
        setTimeout(() => {
            showTypingIndicator();

            $.ajax({
                url: '/Usages/GetDashboardSuggestion',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(messageText),
                success: function (response) {
                    hideTypingIndicator();
                    const hideDurationEstimate = 400;
                    setTimeout(() => {
                        if (response.success) {
                            appendMessage(response.suggestion, 'bot-message', false);
                        } else {
                            appendMessage(response.message || "Sorry, I couldn't get a suggestion right now.", 'bot-message', false);
                        }
                    }, hideDurationEstimate);
                },
                error: function (xhr, status, error) {
                    hideTypingIndicator();
                    const hideDurationEstimate = 400;
                    setTimeout(() => {
                        console.error("AJAX Error:", status, error, xhr.responseText);
                        appendMessage("Sorry, there was an error connecting to the assistant. Please try again.", 'bot-message', false);
                    }, hideDurationEstimate);
                }
            });

        }, showIndicatorDelay);
    }

    // --- Append Message Function (Modified for smoother animation) ---
    function appendMessage(text, messageClass, instant = false) {
        const $messageElement = $('<div></div>').addClass('message ' + messageClass);

        if (messageClass === 'bot-message' && !instant) {
            $chatBody.append($messageElement);
            // Initial scroll to bring bubble into view
            // Scroll happens less frequently inside the loop now
            // scrollToBottom();

            const wordsAndSpaces = text.split(/(\s+)/);
            let wordIndex = 0;
            // Slightly increased delay between words might feel smoother
            const wordFadeDelay = 85; // Adjust this value (try 80-100)
            // How many words to process before forcing a scroll
            const scrollCheckInterval = 5; // Scroll every 5 words/spaces

            function animateNextWord() {
                if (!isChatOpen) return; // Stop if chat closed

                if (wordIndex < wordsAndSpaces.length) {
                    const part = wordsAndSpaces[wordIndex];
                    if (part.trim() !== '') {
                        const $wordSpan = $('<span></span>')
                            .addClass('word-to-animate')
                            .text(part);
                        $messageElement.append($wordSpan);
                        // Trigger opacity transition
                        setTimeout(() => {
                            $wordSpan.css('opacity', 1);
                        }, 10);
                    } else {
                        // Append spaces directly
                        $messageElement.append(document.createTextNode(part));
                    }

                    wordIndex++;

                    // Scroll only every few words/parts OR when done
                    if (wordIndex % scrollCheckInterval === 0 || wordIndex >= wordsAndSpaces.length) {
                        scrollToBottom(); // Use animated scroll
                    }

                    // Schedule next word
                    wordAppendTimeout = setTimeout(animateNextWord, wordFadeDelay);
                } else {
                    // Ensure final scroll when all words are done
                    scrollToBottom();
                }
            }
            // Start animation
            const initialDelayForBubble = 50;
            wordAppendTimeout = setTimeout(animateNextWord, initialDelayForBubble);

        } else { // User messages or history (instant)
            $messageElement.text(text);
            $chatBody.append($messageElement);
            scrollToBottom(instant);
        }
    }

    // --- Typing Indicator Functions ---
    function showTypingIndicator() {
        clearTimeout(typingIndicatorTimeout);
        $('#typing-indicator-element').remove();
        clearTimeout(wordAppendTimeout);

        const $indicatorElement = $('<div></div>')
            .addClass('message bot-message typing-indicator')
            .attr('id', 'typing-indicator-element')
            .html(`
                <span></span>
                <span></span>
                <span></span>
            `);
        $chatBody.append($indicatorElement);
        scrollToBottom(); // Animated scroll for indicator
    }

    function hideTypingIndicator() {
        clearTimeout(typingIndicatorTimeout);
        typingIndicatorTimeout = setTimeout(() => {
            const $indicator = $('#typing-indicator-element');
            if ($indicator.length) {
                $indicator.css('animation', 'messageFadeOut 0.3s ease forwards');
                setTimeout(() => {
                    if ($('#typing-indicator-element').is($indicator)) {
                        $indicator.remove();
                    }
                }, 300);
            }
        }, 100);
    }

    // --- Scroll Function (Unchanged from previous fix) ---
    function scrollToBottom(instant = false) {
        const $container = $chatBody;
        const scrollHeight = $container.prop("scrollHeight");

        if (instant) {
            $container.stop(true, true).scrollTop(scrollHeight);
        } else {
            $container.stop(true, false).animate({
                scrollTop: scrollHeight
            }, 200);
        }
    }

    // (Keep the keyframes check/insert code)
    const styleSheet = document.styleSheets[0];
    let ruleExists = false;
    try {
        if (styleSheet) {
            for (let i = 0; i < styleSheet.cssRules.length; i++) {
                if (styleSheet.cssRules[i].type === CSSRule.KEYFRAMES_RULE && styleSheet.cssRules[i].name === 'messageFadeOut') {
                    ruleExists = true;
                    break;
                }
            }
            if (!ruleExists) {
                styleSheet.insertRule(`
                    @keyframes messageFadeOut {
                        from { opacity: 1; transform: scale(1) translateY(0); }
                        to { opacity: 0; transform: scale(0.9) translateY(10px); }
                    }
                `, styleSheet.cssRules.length);
            }
        }
    } catch (e) {
        // console.warn("Could not check/insert fadeOut keyframes: ", e);
    }
});
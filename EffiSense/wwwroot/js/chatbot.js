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



    function appendMessage(text, messageClass, instant = false) {
        const $rootMessageElement = $('<div></div>').addClass('message ' + messageClass);


        if (messageClass === 'bot-message' && !instant) {
            $chatBody.append($rootMessageElement);

            const fullHtml = parseMarkdown(text);
            const htmlParts = fullHtml.match(/<[^>]+>|[^<]+?(?=<|$)|[^<]+$/g) || [];


            let partIndex = 0;
            let $currentParentElement = $rootMessageElement; 

            const wordFadeDelay = 85;
            const tagAppendDelay = 5; 
            const scrollCheckIntervalWords = 5; 

            function animateNextHtmlPart() {
                if (!isChatOpen || partIndex >= htmlParts.length) {
                    if (partIndex >= htmlParts.length) {
                        scrollToBottom(); 
                    }
                    return;
                }

                const currentHtmlPart = htmlParts[partIndex];

                if (currentHtmlPart.startsWith('</')) { 
                    if ($currentParentElement[0] !== $rootMessageElement[0]) {
                        $currentParentElement = $currentParentElement.parent();
                    }
                    partIndex++;
                    wordAppendTimeout = setTimeout(animateNextHtmlPart, tagAppendDelay);
                } else if (currentHtmlPart.startsWith('<')) {
                    const $newElement = $(currentHtmlPart);
                    $currentParentElement.append($newElement);

                    if (!(currentHtmlPart.endsWith('/>') || ['<br>', '<hr>', '<img>', '<input>'].some(sc => currentHtmlPart.startsWith(sc)))) {
                        $currentParentElement = $newElement;
                    }
                    partIndex++;
                    wordAppendTimeout = setTimeout(animateNextHtmlPart, tagAppendDelay);
                } else { 
                    const wordsAndSpaces = currentHtmlPart.split(/(\s+)/).filter(s => s.length > 0);
                    let wordInChunkIndex = 0;

                    function animateTextChunk() {
                        if (!isChatOpen || wordInChunkIndex >= wordsAndSpaces.length) {
                            if (wordInChunkIndex >= wordsAndSpaces.length) {
                                partIndex++;
                                wordAppendTimeout = setTimeout(animateNextHtmlPart, tagAppendDelay);
                            }
                            return;
                        }

                        const textSegment = wordsAndSpaces[wordInChunkIndex];
                        if (textSegment.trim() !== '') {
                            const $wordSpan = $('<span></span>')
                                .addClass('word-to-animate')
                                .text(textSegment); 
                            $currentParentElement.append($wordSpan);

                            void $wordSpan[0].offsetWidth;
                            $wordSpan.css('opacity', 1);
                        } else {
                            $currentParentElement.append(document.createTextNode(textSegment));
                        }

                        wordInChunkIndex++;

                        if (wordInChunkIndex % scrollCheckIntervalWords === 0 || wordInChunkIndex >= wordsAndSpaces.length) {
                            scrollToBottom();
                        }
                        wordAppendTimeout = setTimeout(animateTextChunk, wordFadeDelay);
                    }
                    animateTextChunk(); 
                }
            }
            const initialDelayForBubble = 50; 
            wordAppendTimeout = setTimeout(animateNextHtmlPart, initialDelayForBubble);

        } else { 
            if (messageClass === 'bot-message') { 
                $rootMessageElement.html(parseMarkdown(text)); 
            } else { 
                $rootMessageElement.text(text);
            }
            $chatBody.append($rootMessageElement);
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
        scrollToBottom(); 
    }
    function parseMarkdown(text) {
        if (typeof text !== 'string') {
            return '';
        }

        let html = text;

        html = html.replace(/^### (.*$)/gim, '<h3>$1</h3>');
        html = html.replace(/^## (.*$)/gim, '<h2>$1</h2>');
        html = html.replace(/^# (.*$)/gim, '<h1>$1</h1>');

        let lines = html.split('\n');
        let newLines = [];
        let inList = false; 
        let listTag = '';   
        let listIndent = ''; 

        for (let i = 0; i < lines.length; i++) {
            let line = lines[i];
            const ulMatch = line.match(/^(\s*)(?:-|\*|\+)\s+(.*)/);
            const olMatch = line.match(/^(\s*)(\d+\.)\s+(.*)/);

            let currentItemProcessed = false;

            if (ulMatch) {
                currentItemProcessed = true;
                const currentItemIndent = ulMatch[1];
                const itemText = ulMatch[2];

                if (!inList || listTag !== 'ul' || currentItemIndent !== listIndent) {
                    if (inList) { 
                        newLines.push(listIndent + '</' + listTag + '>');
                    }
                    listIndent = currentItemIndent;
                    newLines.push(listIndent + '<ul>');
                    listTag = 'ul';
                    inList = true;
                }
                newLines.push(listIndent + '<li>' + itemText + '</li>');
            } else if (olMatch) {
                currentItemProcessed = true;
                const currentItemIndent = olMatch[1];
                const itemText = olMatch[3];

                if (!inList || listTag !== 'ol' || currentItemIndent !== listIndent) {
                    if (inList) { 
                        newLines.push(listIndent + '</' + listTag + '>');
                    }
                    listIndent = currentItemIndent; 
                    newLines.push(listIndent + '<ol>'); 
                    listTag = 'ol';
                    inList = true;
                }
                newLines.push(listIndent + '<li>' + itemText + '</li>');
            }


            if (!currentItemProcessed && inList) {
                newLines.push(listIndent + '</' + listTag + '>');
                inList = false;
                listTag = '';
                listIndent = '';
            }

            if (!currentItemProcessed) {
                newLines.push(line);
            }
        }

        if (inList) { 
            newLines.push(listIndent + '</' + listTag + '>');
        }
        html = newLines.join('\n');

        html = html.replace(/~~(.*?)~~/g, '<del>$1</del>');
        html = html.replace(/\*\*\*(.+?)\*\*\*|___(.+?)___/g, function (match, p1, p2) {
            return '<strong><em>' + (p1 || p2) + '</em></strong>';
        });
        html = html.replace(/\*\*(.+?)\*\*|__(.+?)__/g, function (match, p1, p2) {
            return '<strong>' + (p1 || p2) + '</strong>';
        });
        html = html.replace(/(?<!\w)\*(?!\s)(.*?[^\s])\*(?!\w)|(?<!\w)_(?!\s)(.*?[^\s])_(?!\w)/g, function (match, p1, p2) {
            return '<em>' + (p1 || p2) + '</em>';
        });

        return html;
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
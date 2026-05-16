(function () {
    const dropzones = new WeakMap();

    window.veraciBotDynamicImage = {
        initializeImageDropzone,
        disposeImageDropzone,
        pasteFromClipboard
    };

    function initializeImageDropzone(element, dotNetReference, maxSize) {
        if (!element) {
            return;
        }

        disposeImageDropzone(element);

        const state = {
            dotNetReference,
            maxSize,
            handlers: []
        };

        addHandler(state, element, "dragenter", event => {
            event.preventDefault();
            element.classList.add("is-dragover");
        });

        addHandler(state, element, "dragover", event => {
            event.preventDefault();
            event.dataTransfer.dropEffect = "copy";
            element.classList.add("is-dragover");
        });

        addHandler(state, element, "dragleave", event => {
            if (!element.contains(event.relatedTarget)) {
                element.classList.remove("is-dragover");
            }
        });

        addHandler(state, element, "drop", async event => {
            event.preventDefault();
            element.classList.remove("is-dragover");

            const file = findImageFile(event.dataTransfer?.files);
            if (!file) {
                await notifyError(state, "Solte um arquivo de imagem.");
                return;
            }

            await setInputFile(element, file, state);
        });

        addHandler(state, element, "paste", async event => {
            const file = findImageFile(event.clipboardData?.files) || findImageFileFromItems(event.clipboardData?.items);
            if (!file) {
                await notifyError(state, "Nenhuma imagem encontrada no clipboard.");
                return;
            }

            event.preventDefault();
            await setInputFile(element, file, state);
        });

        dropzones.set(element, state);
    }

    function disposeImageDropzone(element) {
        const state = dropzones.get(element);
        if (!state) {
            return;
        }

        for (const handler of state.handlers) {
            handler.target.removeEventListener(handler.name, handler.callback);
        }

        dropzones.delete(element);
    }

    async function pasteFromClipboard(element, maxSize) {
        const state = dropzones.get(element) || { maxSize };

        if (!navigator.clipboard?.read) {
            await notifyError(state, "Seu navegador nao permite ler o clipboard pelo botao. Clique na area da imagem e use Ctrl+V.");
            return;
        }

        const items = await navigator.clipboard.read();
        for (const item of items) {
            const contentType = item.types.find(type => type.startsWith("image/"));
            if (!contentType) {
                continue;
            }

            const blob = await item.getType(contentType);
            const file = new File([blob], `clipboard-${Date.now()}${extensionFromContentType(contentType)}`, { type: contentType });
            await setInputFile(element, file, state);
            return;
        }

        await notifyError(state, "Nenhuma imagem encontrada no clipboard.");
    }

    function addHandler(state, target, name, callback) {
        target.addEventListener(name, callback);
        state.handlers.push({ target, name, callback });
    }

    async function setInputFile(element, file, state) {
        if (!file.type?.startsWith("image/")) {
            await notifyError(state, "Selecione um arquivo de imagem.");
            return;
        }

        if (state.maxSize && file.size > state.maxSize) {
            await notifyError(state, "A imagem excede o limite de 10 MB.");
            return;
        }

        const input = element.querySelector("input[type='file']");
        if (!input) {
            await notifyError(state, "Campo de upload nao encontrado.");
            return;
        }

        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        input.files = dataTransfer.files;
        input.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function findImageFile(files) {
        if (!files) {
            return null;
        }

        return Array.from(files).find(file => file.type?.startsWith("image/")) || null;
    }

    function findImageFileFromItems(items) {
        if (!items) {
            return null;
        }

        const item = Array.from(items).find(value => value.type?.startsWith("image/"));
        return item?.getAsFile() || null;
    }

    async function notifyError(state, message) {
        if (state?.dotNetReference) {
            await state.dotNetReference.invokeMethodAsync("ReceiveBrowserImageErrorAsync", message);
        }
    }

    function extensionFromContentType(contentType) {
        switch (contentType) {
            case "image/jpeg":
                return ".jpg";
            case "image/gif":
                return ".gif";
            case "image/webp":
                return ".webp";
            case "image/bmp":
                return ".bmp";
            default:
                return ".png";
        }
    }
})();

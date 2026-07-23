class EasyCpdclDtl430 extends BaseInstrument {
    constructor() {
        super();
        this.screen = null;
        this.screenContext = null;
        this.waiting = null;
        this.commBus = null;
    }

    get templateID() {
        return "EasyCPDLC-DTL430";
    }

    connectedCallback() {
        super.connectedCallback();
        this.screen = this.getChildById("dtl430-screen");
        this.waiting = this.getChildById("dtl430-waiting");
        this.screenContext = this.screen.getContext("2d");
        this.screenContext.imageSmoothingEnabled = false;
        this.commBus = RegisterCommBusListener(() => {
            this.commBus.on(
                "EasyCPDLC.DTL430.Display.v1",
                payload => this.receiveDisplay(payload));
        });
    }

    receiveDisplay(payload) {
        let message;
        try {
            message = typeof payload === "string" ? JSON.parse(payload) : payload;
        } catch (_) {
            return;
        }
        if (!message || message.version !== 1 || typeof message.png !== "string") {
            return;
        }

        const image = new Image();
        image.onload = () => {
            this.screenContext.imageSmoothingEnabled = false;
            this.screenContext.clearRect(0, 0, 240, 128);
            this.screenContext.drawImage(image, 0, 0, 240, 128);
            this.waiting.style.display = "none";
        };
        image.src = message.png;
    }
}

registerInstrument("easycpdlc-dtl430", EasyCpdclDtl430);

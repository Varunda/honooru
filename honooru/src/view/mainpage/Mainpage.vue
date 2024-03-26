<template>
    <div>
        <app-menu>
        </app-menu>

        <hr class="border" />

        <div>
            <h1>PLAP</h1>
            <h3>Planetside 2 Lore Archive Project</h3>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import * as sR from "signalR";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";

    import "filters/MomentFilter";

    export const Mainpage = Vue.extend({
        props: {

        },

        created: function(): void {
            this.connection = new sR.HubConnectionBuilder()
                .withUrl("/ws/overview")
                .withAutomaticReconnect([5000, 10000, 20000, 20000])
                .build();

            this.connection.on("UpdateData", (data: any) => {
                this.lastUpdate = new Date();
            });

            this.connection.start().then(() => {
                this.socketState = "opened";
                console.log(`connected`);
            }).catch(err => {
                console.error(err);
            });

            this.connection.onreconnected(() => {
                console.log(`reconnected`);
                this.socketState = "opened";
            });

            this.connection.onclose((err?: Error) => {
                this.socketState = "closed";
                if (err) {
                    console.error("onclose: ", err);
                }
            });

            this.connection.onreconnecting((err?: Error) => {
                this.socketState = "reconnecting";
                if (err) {
                    console.error("onreconnecting: ", err);
                }
            });

            document.title = "Honooru / Homepage";
        },

        data: function() {
            return {
                socketState: "unconnected" as string,
                connection: null as sR.HubConnection | null,
                lastUpdate: null as Date | null,
            }
        },

        methods: {

        },

        components: {
            InfoHover,
            AppMenu,
        }
    });

    export default Mainpage;
</script>
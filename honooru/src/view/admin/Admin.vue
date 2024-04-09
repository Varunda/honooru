
<template>
    <div>
        <app-menu></app-menu>

        <div>
            <div class="border" style="max-width: 300px">
                <h2 class="bg-secondary p-2 mb-2">dev jobs</h2>

                <div class="d-grid p-2 align-items-center" style="grid-template-columns: 1fr min-content; gap: 0.5rem">
                    <div>
                        remake all post thumbnails
                    </div>

                    <button class="btn btn-primary btn-sm py-0" @click="remakeAllThumbnails" id="remake-thumbnails">
                        run
                    </button>

                    <div>
                        remake all IQDB entries
                    </div>

                    <button class="btn btn-primary btn-sm py-0" @click="remakeAllIqdbEntries">
                        run
                    </button>
                </div>

            </div>


        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import Toaster from "Toaster";
    import { Loading, Loadable } from "Loading";

    import { AdminApi } from "api/AdminApi";

    import { AppMenu } from "components/AppMenu";

    export const Admin = Vue.extend({
        props: {

        },

        data: function() {
            return {

            }
        },

        methods: {
            remakeAllThumbnails: async function(): Promise<void> {
                const l: Loading<void> = await AdminApi.remakeAllThumbnails();
                if (l.state == "loaded") {
                    Toaster.add("success", "all thumbnails submitted for recreation", "success");
                } else if (l.state == "error") {
                    Toaster.add("error", `failed to submit thumbnails:<br/><code>${l.problem.title}</code>`, "danger");
                }
            },

            remakeAllIqdbEntries: async function(): Promise<void> {
                const l: Loading<void> = await AdminApi.remakeAllIqdbEntries();
                if (l.state == "loaded") {
                    Toaster.add("success", "submitted all posts for IQDB re-hash", "success");
                } else if (l.state == "error") {
                    Toaster.add("error", `failed to submit IQDB re-hash:<br/><code>${l.problem.title}</code>`, "danger");
                }
            }
        },

        components: {
            AppMenu
        }

    });
    export default Admin;
</script>
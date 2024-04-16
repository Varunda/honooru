
<template>
    <div>
        <app-menu></app-menu>

        <div>
            <div class="border" style="max-width: 400px">
                <h2 class="bg-secondary p-2 mb-2">dev jobs</h2>

                <div class="d-grid p-2 align-items-center" style="grid-template-columns: 1fr max-content; gap: 0.5rem">
                    <div>
                        remake all post thumbnails
                    </div>

                    <button class="btn btn-primary btn-sm py-0" @click="remakeAllThumbnails" id="remake-thumbnails">
                        run
                    </button>

                    <div>
                        remake all IQDB entries
                    </div>

                    <div>
                        <button class="btn btn-primary btn-sm py-0 d-block" @click="remakeAllIqdbEntries(true)">
                            run
                        </button>

                        <button class="btn btn-primary btn-sm py-0 d-block" @click="remakeAllIqdbEntries(false)">
                            run skip
                        </button>
                    </div>

                    <div>
                        recount all tags
                    </div>

                    <button class="btn btn-primary btn-sm py-0" @click="recountTags">
                        run
                    </button>

                    <div>
                        update all file types
                    </div>

                    <button class="btn btn-primary btn-sm py-0" @click="updateFileType">
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

        created: function(): void {
            document.title = "Honooru / Admin";
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

            remakeAllIqdbEntries: async function(force: boolean): Promise<void> {
                const l: Loading<void> = await AdminApi.remakeAllIqdbEntries(force);
                if (l.state == "loaded") {
                    Toaster.add("success", "submitted all posts for IQDB re-hash", "success");
                } else if (l.state == "error") {
                    Toaster.add("error", `failed to submit IQDB re-hash:<br/><code>${l.problem.title}</code>`, "danger");
                }
            },

            recountTags: async function(): Promise<void> {
                const l: Loading<void> = await AdminApi.recountTags();
                if (l.state == "loaded") {
                    Toaster.add("success", "submitted all tags for recount", "success");
                } else if (l.state == "error") {
                    Toaster.add("error", `failed to submit tag recount:<br/><code>${l.problem.title}</code>`, "danger");
                }
            },

            updateFileType: async function(): Promise<void> {
                const l: Loading<void> = await AdminApi.updateFileType();
                if (l.state == "loaded") {
                    Toaster.add("success", "updating all file types", "success");
                } else if (l.state == "error") {
                    Toaster.add("error", `failed to update file types:<br/><code>${l.problem.title}</code>`, "danger");
                }
            },
        },

        components: {
            AppMenu
        }

    });
    export default Admin;
</script>
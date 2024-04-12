
<template>
    <div>
        <app-menu></app-menu>

        <h1 class="wt-header">
            settings
        </h1>

        <h4 class="wt-header bg-dark">
            post list settings
        </h4>

        <div>
            <div class="card mb-2">
                <div class="card-body">
                    <h5 class="card-title">
                        explicit content
                    </h5>

                    <h6 class="card-subtitle mb-2 text-muted">
                        <span>
                            all posts with the rating set to explicit will be:
                        </span>
                        <strong>
                            <span v-if="settings.explicitBehavior == 'shown'">
                                shown
                            </span>
                            <span v-if="settings.explicitBehavior == 'blur'">
                                blurred
                            </span>
                            <span v-if="settings.explicitBehavior == 'hidden'">
                                hidden
                            </span>
                        </strong>
                    </h6>

                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="radio" v-model="settings.explicitBehavior" value="shown" />
                        <label class="form-check-label">
                            shown
                        </label>
                    </div>

                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="radio" v-model="settings.explicitBehavior" value="blur" />
                        <label class="form-check-label">
                            blurred
                        </label>
                    </div>

                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="radio" v-model="settings.explicitBehavior" value="hidden" />
                        <label class="form-check-label">
                            hidden
                        </label>
                    </div>
                </div>
            </div>

            <div class="card">
                <div class="card-body">
                    <h5 class="card-title">
                        unsafe content
                    </h5>

                    <h6 class="card-subtitle mb-2 text-muted">
                        <span>
                            all posts with the rating set to unsafe will be:
                        </span>
                        <strong>
                            <span v-if="settings.unsafeBehavior == 'shown'">
                                shown
                            </span>
                            <span v-if="settings.unsafeBehavior == 'blur'">
                                blurred
                            </span>
                            <span v-if="settings.unsafeBehavior == 'hidden'">
                                hidden
                            </span>
                        </strong>
                    </h6>

                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="radio" v-model="settings.unsafeBehavior" value="shown" />
                        <label class="form-check-label">
                            shown
                        </label>
                    </div>

                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="radio" v-model="settings.unsafeBehavior" value="blur" />
                        <label class="form-check-label">
                            blurred
                        </label>
                    </div>

                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="radio" v-model="settings.unsafeBehavior" value="hidden" />
                        <label class="form-check-label">
                            hidden
                        </label>
                    </div>
                </div>
            </div>

            <div class="card">
                <div class="card-body">
                    <h5 class="card-title">
                        post list count
                    </h5>

                    <h6 class="card-subtitle mb-2 text-muted">
                        how many posts will be returned when searching
                    </h6>

                    <div class="form-group">
                        <input v-model.number="settings.postCount" class="form-control" />
                    </div>
                </div>
            </div>

            <hr class="border" />

            <button class="btn btn-success" @click="save" :disabled="saving.state == 'loading'">
                save
            </button>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import Toaster from "Toaster";
    import { Loading, Loadable } from "Loading";

    import { UserSetting, UserSettingApi } from "api/UserSettingApi";

    import { AppMenu } from "components/AppMenu";

    export const Settings = Vue.extend({
        props: {

        },

        data: function() {
            return {
                data: Loadable.idle() as Loading<UserSetting[]>,
                copy: [] as UserSetting[],

                saving: Loadable.idle() as Loading<void>,

                settings: {
                    explicitBehavior: "" as string,
                    unsafeBehavior: "" as string,

                    postCount: 0 as number
                }
            }
        },

        mounted: function(): void {
            this.loadSettings();
        },

        methods: {

            loadSettings: async function(): Promise<void> {
                this.data = Loadable.loading();
                this.data = await UserSettingApi.getByCurrentUser();

                if (this.data.state != "loaded") {
                    return;
                }

                this.copy = [...this.data.data];

                const map: Map<string, UserSetting> = new Map();
                for (const setting of this.data.data) {
                    map.set(setting.name, setting);
                }

                this.settings.explicitBehavior = map.get("postings.explicit.behavior")?.value ?? "shown";
                this.settings.unsafeBehavior = map.get("postings.unsafe.behavior")?.value ?? "shown";
                this.settings.postCount = Number.parseInt(map.get("postings.count")?.value ?? "10");
            },

            save: async function(): Promise<void> {
                if (this.data.state != "loaded") {
                    console.warn(`cannot save settings, data is not loaded, is "${this.data.state}" instead`);
                    return;
                }

                this.saving = Loadable.loading();

                const needsUpdate: UserSetting[] = [];

                const map: Map<string, UserSetting> = new Map();
                for (const setting of this.data.data) {
                    map.set(setting.name, setting);
                }

                if (map.get("postings.explicit.behavior")?.value != this.settings.explicitBehavior) {
                    await UserSettingApi.update("postings.explicit.behavior", this.settings.explicitBehavior);
                }

                if (map.get("postings.unsafe.behavior")?.value != this.settings.unsafeBehavior) {
                    await UserSettingApi.update("postings.unsafe.behavior", this.settings.unsafeBehavior);
                }

                if (Number.parseInt(map.get("postings.count")?.value ?? "10") != this.settings.postCount) {
                    await UserSettingApi.update("postings.count", this.settings.unsafeBehavior);
                }

                this.saving = Loadable.loaded(undefined);
                await this.loadSettings();
            }
        },

        components: {
            AppMenu
        }

    });
    export default Settings;
</script>
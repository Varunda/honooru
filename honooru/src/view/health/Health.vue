<template>
    <div>
        <app-menu>
            <menu-dropdown></menu-dropdown>

            <menu-sep></menu-sep>

            <li class="nav-item h1 p-0">
                Health
            </li>
        </app-menu>

        <div>
            <h3 class="d-inline">
                Latest update -
                <span v-if="latestUpdate != null">
                    {{latestUpdate | moment("YYYY-MM-DD hh:mm:ss A")}}
                    ::
                    {{latestUpdate | timeAgo}}
                </span>
            </h3>
        </div>

        <div v-if="health.state == 'loaded'">
            <div class="row">
                <div class="col-12">
                    <h1 class="wt-header">Queues</h1>

                    <table class="table table-striped table-sm" style="table-layout: fixed;">
                        <thead>
                            <tr class="table-secondary">
                                <th>Queue</th>
                                <th>Length</th>
                                <th>
                                    Processed
                                    <toggle-button v-model="settings.useCompact" class="btn-sm py-0">
                                        Compact
                                    </toggle-button>
                                </th>
                                <th>Average</th>
                                <th>Median</th>
                                <th>Min</th>
                                <th>Max</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="queue in health.data.queues">
                                <td>{{queue.queueName}}</td>
                                <td>{{queue.count}}</td>
                                <td :title="queue.processed | locale">
                                    <span v-if="settings.useCompact">
                                        {{queue.processed | compact}}
                                    </span>
                                    <span v-else>
                                        {{queue.processed | locale}}
                                    </span>
                                </td>

                                <td>
                                    <span v-if="queue.average != null">
                                        {{queue.average | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                                <td>
                                    <span v-if="queue.median != null">
                                        {{queue.median | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                                <td>
                                    <span v-if="queue.min != null">
                                        {{queue.min | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                                <td>
                                    <span v-if="queue.max != null">
                                        {{queue.max | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                            </tr>
                        </tbody>    
                    </table>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppHealth, AppHealthApi } from "api/AppHealthApi";

    import "filters/MomentFilter";
    import "filters/TimeAgoFilter";
    import "filters/LocaleFilter";
    import "filters/DurationFilter";
    import "filters/CompactFilter";

    import { AppMenu, MenuSep, MenuDropdown, MenuImage } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";

    interface EventQueueEntry {
        when: Date;
        count: number;
    }

    export const Health = Vue.extend({
        props: {

        },

        data: function() {
            return {
                health: Loadable.idle() as Loading<AppHealth>,
                latestUpdate: null as | Date | null,

                timerID: undefined as number | undefined,

                settings: {
                    showGraph: true as boolean,
                    useCompact: true as boolean
                }
            }
        },

        created: function(): void {
            document.title = `Honooru / Health`;

            this.updateHealth();
            this.timerID = setInterval(async () => {
                await this.updateHealth();
            }, 1000) as unknown as number;

            // Force an update in a separate interval from the API update to ensure the time ago
            //      displays are updated. This is useful if you have a poor connection, or nothing
            //      is getting updated for another reason
            setInterval(() => {
                let needsRefresh: boolean = this.latestUpdate == null;

                if (this.latestUpdate != null) {
                    const diff: number = new Date().getTime() - this.latestUpdate.getTime();

                    // Only refresh if new data hasn't been found in 1500ms. This prevents flickering
                    //      if the two intervals are out of sync. For example, if the updateHealth interval
                    //      ran every 500ms, then this interval ran 500ms after the updateHealth interval,
                    //      this would cause the data to flicker every 500ms, very annoying
                    if (diff > 1500) {
                        console.warn(`data is ${diff}ms old, forcing a refresh`);
                        needsRefresh = true;
                    }
                }

                if (needsRefresh == true) {
                    this.$forceUpdate();
                }
            }, 1000);
        },

        methods: {
            updateHealth: async function(): Promise<void> {
                this.health = await AppHealthApi.getHealth();
                if (this.health.state == "loaded") {
                    this.latestUpdate = new Date();
                }
            },
        },

        computed: {

        },

        components: {
            AppMenu, MenuSep, MenuDropdown, MenuImage,
            InfoHover, ToggleButton,
        }
    });
    export default Health;
</script>

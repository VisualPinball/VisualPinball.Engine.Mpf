"""Widget player plugin for MPF to support GMC."""
from functools import partial
from mpf.config_players.plugin_player import PluginPlayer


class WidgetPlayer(PluginPlayer):

    """Widget Player in MPF.

    Note: This class is loaded by MPF and everything in it is in the context of MPF.
    """

    config_file_section = 'widget_player'
    show_section = 'widgets'

    def _validate_config_item(self, device, device_settings):
        # device is slide name, device_settings is configuration

        device_settings = self.machine.config_validator.validate_config(
            'widget_player', device_settings)

        return_dict = dict()
        return_dict[device] = device_settings

        return return_dict

    def _register_trigger(self, event, **kwargs):
        """Register trigger via BCP for MC."""
        del kwargs
        client = self.machine.bcp.transport.get_named_client("local_display")
        if client:
            self.machine.bcp.interface.add_registered_trigger_event_for_client(client, event)
        else:
            self.machine.events.add_handler("bcp_clients_connected", partial(self._register_trigger, event))
